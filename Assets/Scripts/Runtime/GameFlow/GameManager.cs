using System;
using System.Collections.Generic;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Objectives;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Runtime.GameFlow
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager s_Instance;
        public static GameManager Instance => s_Instance;

        public UnityAction<GameState> OnStateChanged;
        public UnityAction<bool> OnPauseChanged;
        public UnityAction OnLevelRestarted;

        private GameState m_State = GameState.Loading;
        public GameState State => m_State;

        private bool m_IsPaused;
        public bool IsPaused => m_IsPaused;

        [SerializeField] private LevelController m_LevelController;
        [SerializeField] private BallStream m_BallStream;
        [SerializeField] private ObjectiveTracker m_ObjectiveTracker;

        private readonly Dictionary<GameState, IState> m_States = new Dictionary<GameState, IState>();

        public void SetReferences(LevelController levelController, BallStream ballStream, ObjectiveTracker objectiveTracker = null)
        {
            UnwireEvents();

            m_LevelController = levelController;
            m_BallStream = ballStream;
            m_ObjectiveTracker = objectiveTracker;

            WireEvents();
        }

        private void Start()
        {
            if (m_LevelController == null) m_LevelController = FindFirstObjectByType<LevelController>();
            if (m_BallStream == null) m_BallStream = FindFirstObjectByType<BallStream>();
            if (m_ObjectiveTracker == null) m_ObjectiveTracker = FindFirstObjectByType<ObjectiveTracker>();

            WireEvents();

            if (m_LevelController != null && m_LevelController.State != null)
            {
                ChangeState(GameState.Ready);
            }
        }

        private void WireEvents()
        {
            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt -= HandleLevelBuilt;
                m_LevelController.OnLevelBuilt += HandleLevelBuilt;

                m_LevelController.OnDetonationCompleted -= HandleDetonationCompleted;
                m_LevelController.OnDetonationCompleted += HandleDetonationCompleted;

                m_LevelController.OnBoardCleared -= HandleBoardCleared;
                m_LevelController.OnBoardCleared += HandleBoardCleared;
            }

            if (m_BallStream != null)
            {
                m_BallStream.OnBallFired -= HandleBallFired;
                m_BallStream.OnBallFired += HandleBallFired;

                m_BallStream.OnBallReturned -= HandleBallReturned;
                m_BallStream.OnBallReturned += HandleBallReturned;

                m_BallStream.OnDetonationHit -= HandleDetonationHit;
                m_BallStream.OnDetonationHit += HandleDetonationHit;
            }
        }

        private void UnwireEvents()
        {
            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt -= HandleLevelBuilt;
                m_LevelController.OnDetonationCompleted -= HandleDetonationCompleted;
                m_LevelController.OnBoardCleared -= HandleBoardCleared;
            }

            if (m_BallStream != null)
            {
                m_BallStream.OnBallFired -= HandleBallFired;
                m_BallStream.OnBallReturned -= HandleBallReturned;
                m_BallStream.OnDetonationHit -= HandleDetonationHit;
            }
        }

        private void HandleLevelBuilt(LevelDefinition levelDef)
        {
            ChangeState(GameState.Ready);
        }

        private void HandleBallFired(Game.Runtime.Ball.Ball ball)
        {
            ChangeState(GameState.Firing);
        }

        private void HandleDetonationHit(Block block)
        {
            ChangeState(GameState.Settling);
        }

        private void HandleBallReturned()
        {
            if (m_State != GameState.Won)
            {
                ChangeState(GameState.Ready);
            }
        }

        private void HandleDetonationCompleted(ChainResult result)
        {
            // Last brick gone: stay in Settling until the board-clear collapse finishes and
            // OnBoardCleared fires, so Won (and its popup) follows the presentation (§5.1 point 2).
            if (m_LevelController != null && m_LevelController.State != null && m_LevelController.State.BricksRemaining <= 0)
            {
                return;
            }

            if (m_State != GameState.Won)
            {
                ChangeState(GameState.Ready);
            }
        }

        private void HandleBoardCleared()
        {
            ChangeState(GameState.Won);
        }

        private void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this;
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            UnwireEvents();
            if (m_IsPaused)
            {
                // Never leak a frozen timeScale into the next scene
                Time.timeScale = 1f;
            }
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        public void ChangeState(GameState newState)
        {
            if (m_State == newState) return;

            if (m_States.TryGetValue(m_State, out var currentState))
            {
                currentState?.Exit();
            }

            m_State = newState;

            if (m_States.TryGetValue(m_State, out var nextState))
            {
                nextState?.Enter();
            }

            OnStateChanged?.Invoke(m_State);
        }

        /// <summary>
        /// Pause is a gate, not an FSM state (§5.1 point 1): stops time and InputReader
        /// refuses aim/fire/fast-forward while it is set. UI animates on unscaled time.
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (m_IsPaused == paused) return;

            m_IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            OnPauseChanged?.Invoke(m_IsPaused);
        }

        /// <summary>
        /// Instant, free restart (gameplay.md §3 rule 13): drops the ball in flight and rebuilds
        /// the board from the already-loaded LevelDefinition — no scene reload, no dialog.
        /// </summary>
        public void RestartLevel()
        {
            if (m_LevelController == null || m_LevelController.CurrentLevel == null) return;

            LoadLevel(m_LevelController.CurrentLevel);
            OnLevelRestarted?.Invoke();
        }

        /// <summary>Switches the board to another level in place (Replay / Next from the win popup).</summary>
        public void LoadLevel(LevelDefinition levelDef)
        {
            if (levelDef == null || m_LevelController == null) return;

            SetPaused(false);

            if (m_BallStream != null)
            {
                m_BallStream.CancelShot();
            }

            ChangeState(GameState.Loading);
            // OnLevelBuilt → HandleLevelBuilt → Ready
            m_LevelController.Build(levelDef);
        }
    }
}
