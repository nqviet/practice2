using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Board
{
    public class LevelController : MonoBehaviour
    {
        [SerializeField] private Transform m_GridOrigin;
        [SerializeField] private Transform m_BlocksParent;
        [SerializeField] private Transform m_ObstaclesParent;
        [SerializeField] private Block m_BlockPrefab;
        [SerializeField] private LevelDefinition m_CurrentLevel;
        [SerializeField] private GameplayConfig m_Config;

        private BoardState m_BoardState;
        private readonly Dictionary<Vector2Int, Block> m_ActiveBlocks = new Dictionary<Vector2Int, Block>();
        private int m_HeldSettleTokens;

        public BoardState State => m_BoardState;
        public LevelDefinition CurrentLevel => m_CurrentLevel;
        public Transform GridOrigin => m_GridOrigin;
        public GameplayConfig Config => m_Config;

        public UnityAction<ChainResult> OnDetonation;
        public UnityAction<ChainResult> OnDetonationCompleted;
        public UnityAction OnBoardCleared;
        public UnityAction<LevelDefinition> OnLevelBuilt;

        private void Start()
        {
            if (m_CurrentLevel != null && m_BoardState == null)
            {
                Build(m_CurrentLevel);
            }
        }

        public void SetReferences(Transform gridOrigin, Transform blocksParent, Transform obstaclesParent, Block blockPrefab, GameplayConfig config = null)
        {
            m_GridOrigin = gridOrigin;
            m_BlocksParent = blocksParent;
            m_ObstaclesParent = obstaclesParent;
            m_BlockPrefab = blockPrefab;
            if (config != null) m_Config = config;
        }

        public void SetConfig(GameplayConfig config)
        {
            m_Config = config;
        }

        /// <summary>Selects the level Start() builds, without building it now.</summary>
        public void SetLevel(LevelDefinition levelDef)
        {
            m_CurrentLevel = levelDef;
        }

        public void Build(LevelDefinition levelDef)
        {
            if (levelDef == null)
            {
                Debug.LogWarning("[LevelController] Cannot build: LevelDefinition is null.");
                return;
            }

            m_CurrentLevel = levelDef;
            ClearBoard();

            Vector3 originPos = m_GridOrigin != null ? m_GridOrigin.position : new Vector3(GameConstants.GridOriginX, GameConstants.GridOriginY, 0f);
            m_BoardState = levelDef.Parse(out List<string> errors);

            if (errors.Count > 0)
            {
                for (int i = 0; i < errors.Count; i++)
                {
                    Debug.LogError($"[LevelController] Parse error: {errors[i]}");
                }
            }

            Transform blocksRoot = m_BlocksParent != null ? m_BlocksParent : transform;
            Transform obstaclesRoot = m_ObstaclesParent != null ? m_ObstaclesParent : blocksRoot;

            foreach (Vector2Int cell in m_BoardState.Cells)
            {
                BlockKind kind = m_BoardState.KindAt(cell);
                if (kind == BlockKind.Empty) continue;

                Vector3 worldPos = GridMath.CellToWorld(cell, originPos);
                Transform parent = (kind == BlockKind.Steel) ? obstaclesRoot : blocksRoot;

                Block blockInstance;
                if (m_BlockPrefab != null)
                {
                    blockInstance = Instantiate(m_BlockPrefab, worldPos, Quaternion.identity, parent);
                }
                else
                {
                    var go = new GameObject($"Block_{cell.x}_{cell.y}");
                    go.transform.position = worldPos;
                    go.transform.SetParent(parent);
                    go.layer = GameConstants.BlockLayer;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingLayerName = GameConstants.SortingBlocks;
                    var col = go.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(GameConstants.CellSize, GameConstants.CellSize);
                    blockInstance = go.AddComponent<Block>();
                }

                BlockDefinition blockDef = null;
                if (levelDef.Palette != null)
                {
                    char c = GetCharForKindAndColor(kind, m_BoardState.ColorAt(cell));
                    levelDef.Palette.TryResolve(c, out blockDef, out _);
                }

                blockInstance.Init(cell, blockDef);
                m_ActiveBlocks[cell] = blockInstance;
            }

            Debug.Log($"[LevelController] Built level '{levelDef.LevelId}' with {m_ActiveBlocks.Count} active blocks. Bricks remaining: {m_BoardState.BricksRemaining}");
            OnLevelBuilt?.Invoke(levelDef);
        }

        public void ResetBoard()
        {
            if (m_CurrentLevel != null)
            {
                Build(m_CurrentLevel);
            }
        }

        public void ClearBoard()
        {
            // Restart / level switch mid-detonation: stop playback so it cannot shatter the rebuilt board,
            // and hand back any settle token the interrupted routine still holds (its finally never runs).
            StopAllCoroutines();
            if (m_HeldSettleTokens > 0)
            {
                var vfxService = ServiceLocator.Get<IVFXService>();
                for (int i = 0; i < m_HeldSettleTokens; i++)
                {
                    vfxService?.ReleaseSettleToken();
                }
                m_HeldSettleTokens = 0;
            }

            foreach (var kvp in m_ActiveBlocks)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            m_ActiveBlocks.Clear();
            m_BoardState = null;
        }

        public Block GetBlockAt(Vector2Int cell)
        {
            m_ActiveBlocks.TryGetValue(cell, out Block block);
            return block;
        }

        public void ProcessDetonation(Vector2Int seed, GameColor ballColor)
        {
            if (m_BoardState == null) return;

            ChainResult result = ChainSolver.Solve(m_BoardState, seed, ballColor);
            ProcessDetonationResult(result);
        }

        public void ProcessDetonationResult(ChainResult result)
        {
            if (result == null || result.DestroyedCells.Count == 0) return;

            OnDetonation?.Invoke(result);
            StartCoroutine(DetonationPlaybackRoutine(result));
        }

        public void SetBoardStateForTest(BoardState state)
        {
            m_BoardState = state;
        }

        private IEnumerator DetonationPlaybackRoutine(ChainResult result)
        {
            float freezeSec = m_Config != null ? m_Config.DetonationFreezeSec : 0.04f;
            float staggerSec = m_Config != null ? m_Config.ChainStaggerSec : 0.05f;

            var vfx = ServiceLocator.Get<IVFXService>();
            var audio = ServiceLocator.Get<IAudioService>();
            var shaker = ServiceLocator.Get<ICameraShaker>();

            vfx?.RetainSettleToken();
            m_HeldSettleTokens++;

            try
            {
                // 1. Detonation Freeze: 40 ms hold, unscaled time, per-system (§6.4)
                if (freezeSec > 0f)
                {
                    yield return new WaitForSecondsRealtime(freezeSec);
                }

                // 2. Camera impulse scaled to destruction size (0.10 - 0.25)
                float impulse = Mathf.Clamp(0.10f + result.DestroyedCells.Count * 0.02f, 0.10f, 0.25f);
                shaker?.Shake(impulse);

                // 3. Bucket cells by depth
                var depthBuckets = new Dictionary<int, List<Vector2Int>>();
                int maxDepth = 0;
                for (int i = 0; i < result.DestroyedCells.Count; i++)
                {
                    Vector2Int cell = result.DestroyedCells[i];
                    int depth = 0;
                    if (result.DepthOf != null && result.DepthOf.TryGetValue(cell, out int d))
                    {
                        depth = d;
                    }
                    if (depth > maxDepth) maxDepth = depth;

                    if (!depthBuckets.TryGetValue(depth, out var list))
                    {
                        list = new List<Vector2Int>();
                        depthBuckets[depth] = list;
                    }
                    list.Add(cell);
                }

                // 4. Staggered playback per depth level (§4.1, §6.4)
                for (int d = 0; d <= maxDepth; d++)
                {
                    if (d > 0 && staggerSec > 0f)
                    {
                        yield return new WaitForSeconds(staggerSec);
                    }

                    if (depthBuckets.TryGetValue(d, out var cellsAtDepth))
                    {
                        // Audio feedback: detonation crack at d=0, ascending combo pitch ladder at d>0
                        if (d == 0)
                        {
                            audio?.Play(SfxId.Detonation);
                        }
                        else
                        {
                            audio?.PlayChainCombo(d);
                        }

                        for (int i = 0; i < cellsAtDepth.Count; i++)
                        {
                            Vector2Int cell = cellsAtDepth[i];
                            if (m_ActiveBlocks.TryGetValue(cell, out Block block))
                            {
                                Vector3 worldPos = block.transform.position;
                                GameColor blockColor = block.Color;
                                m_BoardState?.ClearCell(cell);
                                block.Shatter(d > 0);
                                m_ActiveBlocks.Remove(cell);

                                vfx?.PlayBreak(worldPos, blockColor, d);
                            }
                        }
                    }
                }

                OnDetonationCompleted?.Invoke(result);

                // 5. Win Check: when BricksRemaining <= 0 (§3 rule 11, §6.4)
                if (m_BoardState != null && m_BoardState.BricksRemaining <= 0)
                {
                    // Board clear collapse: staggered 40 ms per column (§6.4)
                    yield return StartCoroutine(BoardClearCollapseRoutine());

                    audio?.Play(SfxId.Win);
                    audio?.DuckMusic(2.0f, 0.15f);

                    var feedback = ServiceLocator.Get<IScreenFeedback>();
                    feedback?.PulseVignette(new Color(0.2f, 0.8f, 1f, 0.4f), 0.6f);

                    OnBoardCleared?.Invoke();
                }
            }
            finally
            {
                if (m_HeldSettleTokens > 0)
                {
                    m_HeldSettleTokens--;
                    vfx?.ReleaseSettleToken();
                }
            }
        }

        private IEnumerator BoardClearCollapseRoutine()
        {
            const float colStaggerSec = 0.04f; // 40 ms per column (§6.4)

            // Group remaining active blocks (steel or leftover colored blocks) by column
            var colBuckets = new Dictionary<int, List<Block>>();
            foreach (var kvp in m_ActiveBlocks)
            {
                if (kvp.Value == null) continue;
                int col = kvp.Key.x;
                if (!colBuckets.TryGetValue(col, out var list))
                {
                    list = new List<Block>();
                    colBuckets[col] = list;
                }
                list.Add(kvp.Value);
            }

            for (int col = 0; col < GameConstants.GridColumns; col++)
            {
                if (colBuckets.TryGetValue(col, out var blocksInCol))
                {
                    for (int i = 0; i < blocksInCol.Count; i++)
                    {
                        var block = blocksInCol[i];
                        if (block != null)
                        {
                            block.Shatter(true);
                        }
                    }
                }
                yield return new WaitForSeconds(colStaggerSec);
            }

            m_ActiveBlocks.Clear();
        }

        private static char GetCharForKindAndColor(BlockKind kind, GameColor color)
        {
            switch (kind)
            {
                case BlockKind.Brick: return 'N';
                case BlockKind.Steel: return 'S';
                case BlockKind.Colored:
                    if (color == GameColor.Red) return 'R';
                    if (color == GameColor.Blue) return 'B';
                    if (color == GameColor.Yellow) return 'Y';
                    return '.';
                default: return '.';
            }
        }
    }
}
