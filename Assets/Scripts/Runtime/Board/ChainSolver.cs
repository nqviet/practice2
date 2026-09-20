using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Board
{
    public class ChainResult
    {
        private readonly List<Vector2Int> m_DestroyedCells;
        private readonly Dictionary<Vector2Int, int> m_DepthOf;
        private readonly int m_BricksDestroyed;
        private readonly int m_ColoredDestroyed;

        public IReadOnlyList<Vector2Int> DestroyedCells => m_DestroyedCells;
        public IReadOnlyDictionary<Vector2Int, int> DepthOf => m_DepthOf;
        public int BricksDestroyed => m_BricksDestroyed;
        public int ColoredDestroyed => m_ColoredDestroyed;

        public ChainResult(List<Vector2Int> destroyedCells, Dictionary<Vector2Int, int> depthOf, int bricksDestroyed, int coloredDestroyed)
        {
            m_DestroyedCells = destroyedCells ?? new List<Vector2Int>();
            m_DepthOf = depthOf ?? new Dictionary<Vector2Int, int>();
            m_BricksDestroyed = bricksDestroyed;
            m_ColoredDestroyed = coloredDestroyed;
        }

        public static ChainResult Empty => new ChainResult(new List<Vector2Int>(), new Dictionary<Vector2Int, int>(), 0, 0);
    }

    public static class ChainSolver
    {
        public static ChainResult Solve(BoardState grid, Vector2Int seed, GameColor ballColor)
        {
            if (grid == null || !GridMath.InGrid(seed))
            {
                return ChainResult.Empty;
            }

            if (grid.KindAt(seed) != BlockKind.Colored || !Matches(ballColor, grid.ColorAt(seed)))
            {
                return ChainResult.Empty;
            }

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            var destroyed = new List<Vector2Int>();
            var destroyedSet = new HashSet<Vector2Int>();
            var depthOf = new Dictionary<Vector2Int, int>();

            queue.Enqueue(seed);
            visited.Add(seed);

            int depth = 0;
            int bricksDestroyed = 0;
            int coloredDestroyed = 0;

            while (queue.Count > 0)
            {
                int count = queue.Count;
                var nextQueue = new List<Vector2Int>();

                for (int i = 0; i < count; i++)
                {
                    Vector2Int c = queue.Dequeue();

                    if (!destroyedSet.Contains(c))
                    {
                        destroyed.Add(c);
                        destroyedSet.Add(c);
                        depthOf[c] = depth;
                        coloredDestroyed++;
                    }

                    GameColor targetColor = grid.ColorAt(c);

                    foreach (Vector2Int n in GridMath.Ring8(c))
                    {
                        BlockKind kind = grid.KindAt(n);

                        if (kind == BlockKind.Brick)
                        {
                            if (!destroyedSet.Contains(n))
                            {
                                destroyed.Add(n);
                                destroyedSet.Add(n);
                                depthOf[n] = depth;
                                bricksDestroyed++;
                            }
                        }
                        else if (kind == BlockKind.Colored)
                        {
                            if (grid.ColorAt(n) == targetColor && !visited.Contains(n))
                            {
                                visited.Add(n);
                                nextQueue.Add(n);
                            }
                        }
                        // Steel ('S'), Empty ('.'), and other-colored blocks are inert and not destroyed
                    }
                }

                for (int j = 0; j < nextQueue.Count; j++)
                {
                    queue.Enqueue(nextQueue[j]);
                }

                depth++;
            }

            return new ChainResult(destroyed, depthOf, bricksDestroyed, coloredDestroyed);
        }

        public static bool Matches(GameColor ballColor, GameColor blockColor)
        {
            if (ballColor == GameColor.White) return true; // Wildcard ball matches any color
            return ballColor == blockColor && ballColor != GameColor.None;
        }
    }
}
