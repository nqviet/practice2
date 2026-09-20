using System.Collections.Generic;
using Game.Board;
using Game.Core;
using Game.Runtime.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Runtime.Ball
{
    public class BallStream : MonoBehaviour
    {
        [SerializeField] private GameplayConfig m_Config;
        [SerializeField] private Ball m_BallPrefab;
        [SerializeField] private Transform m_MuzzlePoint;
        [SerializeField] private LevelController m_LevelController;
        [SerializeField] private PoolService m_PoolService;
        [SerializeField] private List<BallSkin> m_BallSkins = new List<BallSkin>();

        private GameColor m_LoadedKind = GameColor.Red;
        private Ball m_CurrentBall;
        private float m_ShotTimer;
        private bool m_IsFastForward;

        public GameColor LoadedKind
        {
            get => m_LoadedKind;
            set
            {
                if (m_LoadedKind != value)
                {
                    m_LoadedKind = value;
                    OnLoadedKindChanged?.Invoke(m_LoadedKind);
                }
            }
        }

        public bool IsBallInFlight => m_CurrentBall != null && m_CurrentBall.IsInFlight;
        public Ball CurrentBall => m_CurrentBall;
        public GameplayConfig Config => m_Config;

        public UnityAction<GameColor> OnLoadedKindChanged;
        public UnityAction<Ball> OnBallFired;
        public UnityAction OnBallReturned;
        public UnityAction<Block> OnDetonationHit;

        public void SetReferences(
            GameplayConfig config,
            Ball ballPrefab,
            Transform muzzlePoint,
            LevelController levelController,
            PoolService poolService)
        {
            m_Config = config;
            m_BallPrefab = ballPrefab;
            m_MuzzlePoint = muzzlePoint;
            m_LevelController = levelController;
            m_PoolService = poolService;
        }

        public void SetSkins(IEnumerable<BallSkin> skins)
        {
            m_BallSkins.Clear();
            if (skins != null)
            {
                m_BallSkins.AddRange(skins);
            }
        }

        public void Fire(Vector2 direction)
        {
            if (IsBallInFlight)
            {
                return;
            }

            Vector3 spawnPos = m_MuzzlePoint != null ? m_MuzzlePoint.position : transform.position;
            Ball ball = SpawnBall(spawnPos);
            if (ball == null)
            {
                Debug.LogError("[BallStream] Failed to spawn ball.");
                return;
            }

            m_CurrentBall = ball;
            m_ShotTimer = 0f;
            // Fast-forward is per shot: a tap during the previous shot must not carry over
            m_IsFastForward = false;

            float speed = m_Config != null ? m_Config.BallSpeed : 14.0f;
            float returnLineY = GameConstants.ReturnLineY;
            float returnMargin = m_Config != null ? m_Config.ReturnMargin : 0.35f;

            ball.Configure(speed, returnLineY, returnMargin);

            ball.OnBlockHit = HandleBlockHit;
            ball.OnReturned = HandleBallReturned;

            BallSkin skin = GetSkinForColor(m_LoadedKind);
            ball.Launch(direction, speed, m_LoadedKind, skin);

            if (m_IsFastForward && m_Config != null)
            {
                ball.SetFastForward(true, m_Config.FastForwardMultiplier);
            }

            ServiceLocator.Get<IAudioService>()?.Play(SfxId.Fire);
            ServiceLocator.Get<IVFXService>()?.PlayMuzzleFlash(spawnPos, direction);

            OnBallFired?.Invoke(ball);
        }

        public void Recall()
        {
            if (m_CurrentBall != null)
            {
                m_CurrentBall.OnBlockHit = null;
                m_CurrentBall.OnReturned = null;
                m_CurrentBall.Stop();

                DespawnBall(m_CurrentBall);
                m_CurrentBall = null;
            }

            m_ShotTimer = 0f;
            OnBallReturned?.Invoke();
        }

        /// <summary>
        /// Silently removes the ball in flight (restart / level switch). Unlike Recall it raises
        /// no OnBallReturned, so the FSM is not bounced through Ready while the board is rebuilt.
        /// </summary>
        public void CancelShot()
        {
            if (m_CurrentBall != null)
            {
                m_CurrentBall.OnBlockHit = null;
                m_CurrentBall.OnReturned = null;
                m_CurrentBall.Stop();

                DespawnBall(m_CurrentBall);
                m_CurrentBall = null;
            }

            m_ShotTimer = 0f;
            m_IsFastForward = false;
        }

        public void SetFastForward(bool enabled)
        {
            m_IsFastForward = enabled;
            if (m_CurrentBall != null && m_Config != null)
            {
                m_CurrentBall.SetFastForward(enabled, m_Config.FastForwardMultiplier);
            }
        }

        private void Update()
        {
            if (IsBallInFlight)
            {
                m_ShotTimer += Time.deltaTime;
                float timeout = m_Config != null ? m_Config.ShotTimeoutSec : 12.0f;

                if (m_ShotTimer >= timeout)
                {
                    Debug.Log($"[BallStream] Shot timeout reached ({timeout}s). Recalling ball.");
                    Recall();
                }
            }
        }

        private void HandleBlockHit(Block block)
        {
            if (block == null) return;

            bool isWildcard = m_LoadedKind == GameColor.White;
            bool isMatchingColor = block.Color == m_LoadedKind;

            if (block.Kind == BlockKind.Colored && (isWildcard || isMatchingColor))
            {
                // Matching colored detonator hit! Ball is consumed by detonation (§5.2)
                if (m_CurrentBall != null)
                {
                    m_CurrentBall.OnBlockHit = null;
                    m_CurrentBall.OnReturned = null;
                    m_CurrentBall.Stop();

                    DespawnBall(m_CurrentBall);
                    m_CurrentBall = null;
                }

                if (m_LevelController != null)
                {
                    m_LevelController.ProcessDetonation(block.Cell, m_LoadedKind);
                }

                OnDetonationHit?.Invoke(block);
            }
            // Mismatched blocks, bricks, and steel bounce naturally via Ball physics
        }

        private void HandleBallReturned()
        {
            if (m_CurrentBall != null)
            {
                m_CurrentBall.OnBlockHit = null;
                m_CurrentBall.OnReturned = null;
                m_CurrentBall.Stop();

                DespawnBall(m_CurrentBall);
                m_CurrentBall = null;
            }

            m_ShotTimer = 0f;
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.BallReturn);
            ServiceLocator.Get<IScreenFeedback>()?.Flash(new Color(1f, 0.35f, 0.35f, 0.3f), 0.2f);
            OnBallReturned?.Invoke();
        }

        private Ball SpawnBall(Vector3 position)
        {
            if (m_PoolService == null)
            {
                m_PoolService = PoolService.Instance;
            }

            if (m_PoolService != null && m_BallPrefab != null)
            {
                return m_PoolService.Spawn(m_BallPrefab, position, Quaternion.identity);
            }
            else if (m_BallPrefab != null)
            {
                GameObject go = Instantiate(m_BallPrefab.gameObject, position, Quaternion.identity);
                return go.GetComponent<Ball>();
            }
            else
            {
                // Fallback runtime ball
                var go = new GameObject("Ball_Procedural");
                go.transform.position = position;
                var ball = go.AddComponent<Ball>();
                return ball;
            }
        }

        private void DespawnBall(Ball ball)
        {
            if (ball == null) return;

            if (m_PoolService == null)
            {
                m_PoolService = PoolService.Instance;
            }

            if (m_PoolService != null)
            {
                m_PoolService.Despawn(ball);
            }
            else
            {
                Destroy(ball.gameObject);
            }
        }

        public BallSkin GetSkinForColor(GameColor color)
        {
            for (int i = 0; i < m_BallSkins.Count; i++)
            {
                if (m_BallSkins[i] != null && m_BallSkins[i].Color == color)
                {
                    return m_BallSkins[i];
                }
            }
            return null;
        }
    }
}
