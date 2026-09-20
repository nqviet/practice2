using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Runtime.Objectives
{
    public class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private LevelController m_LevelController;
        [SerializeField] private BallStream m_BallStream;

        private int m_BricksRemaining;
        private bool m_HasWon;
        private int m_ShotsTaken;

        public int BricksRemaining => m_BricksRemaining;
        public bool HasWon => m_HasWon;

        /// <summary>Shots fired since the level was (re)built — the "Cleared in N shots" stat (gameplay.md §9 #4).</summary>
        public int ShotsTaken => m_ShotsTaken;

        public UnityAction OnWin;
        public UnityAction<int> OnBricksRemainingChanged;
        public UnityAction<int> OnShotsTakenChanged;

        public void SetBallStream(BallStream ballStream)
        {
            if (m_BallStream != null)
            {
                m_BallStream.OnBallFired -= HandleBallFired;
            }

            m_BallStream = ballStream;

            if (m_BallStream != null && Application.isPlaying)
            {
                m_BallStream.OnBallFired += HandleBallFired;
            }
        }

        private void Start()
        {
            if (m_BallStream == null)
            {
                m_BallStream = FindFirstObjectByType<BallStream>();
            }

            if (m_BallStream != null)
            {
                m_BallStream.OnBallFired -= HandleBallFired;
                m_BallStream.OnBallFired += HandleBallFired;
            }
        }

        private void HandleBallFired(Ball.Ball ball)
        {
            if (m_HasWon) return;

            m_ShotsTaken++;
            OnShotsTakenChanged?.Invoke(m_ShotsTaken);
        }

        public void SetReferences(LevelController levelController)
        {
            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt -= HandleLevelBuilt;
                m_LevelController.OnDetonation -= HandleDetonation;
            }

            m_LevelController = levelController;

            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt += HandleLevelBuilt;
                m_LevelController.OnDetonation += HandleDetonation;

                if (m_LevelController.State != null)
                {
                    m_BricksRemaining = m_LevelController.State.BricksRemaining;
                    m_HasWon = m_BricksRemaining <= 0;
                }
            }
        }

        private void Awake()
        {
            if (m_LevelController == null)
            {
                m_LevelController = FindFirstObjectByType<LevelController>();
            }

            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt += HandleLevelBuilt;
                m_LevelController.OnDetonation += HandleDetonation;
            }
        }

        private void OnDestroy()
        {
            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt -= HandleLevelBuilt;
                m_LevelController.OnDetonation -= HandleDetonation;
            }

            if (m_BallStream != null)
            {
                m_BallStream.OnBallFired -= HandleBallFired;
            }
        }

        private void HandleLevelBuilt(LevelDefinition levelDef)
        {
            if (m_LevelController != null && m_LevelController.State != null)
            {
                m_BricksRemaining = m_LevelController.State.BricksRemaining;
            }
            else
            {
                m_BricksRemaining = 0;
            }

            m_HasWon = false;
            m_ShotsTaken = 0;
            OnBricksRemainingChanged?.Invoke(m_BricksRemaining);
            OnShotsTakenChanged?.Invoke(m_ShotsTaken);
        }

        private void HandleDetonation(ChainResult result)
        {
            if (result == null || m_HasWon) return;

            if (result.BricksDestroyed > 0)
            {
                m_BricksRemaining = Mathf.Max(0, m_BricksRemaining - result.BricksDestroyed);
                OnBricksRemainingChanged?.Invoke(m_BricksRemaining);

                if (m_BricksRemaining <= 0)
                {
                    m_HasWon = true;
                    OnWin?.Invoke();
                }
            }
        }

        public void ResetTracker(int totalBricks)
        {
            m_BricksRemaining = totalBricks;
            m_HasWon = m_BricksRemaining <= 0;
            m_ShotsTaken = 0;
            OnBricksRemainingChanged?.Invoke(m_BricksRemaining);
            OnShotsTakenChanged?.Invoke(m_ShotsTaken);
        }
    }
}
