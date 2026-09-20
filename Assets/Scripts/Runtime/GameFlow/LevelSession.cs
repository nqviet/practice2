using System.Collections.Generic;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Objectives;
using Game.Runtime.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Game.Runtime.GameFlow
{
    /// <summary>
    /// Meta loop for 02_Gameplay: picks the level to play from the LevelPack + save, keeps the
    /// ball picker in sync with the board, records the win (best shots, unlocks) and drives the
    /// LevelComplete popup's Replay / Next / Menu. Replay and Next rebuild in place — no scene
    /// reload (technical_design.md §2 lifetime rules).
    /// </summary>
    public class LevelSession : MonoBehaviour
    {
        [SerializeField] private LevelPack m_LevelPack;
        [SerializeField] private LevelController m_LevelController;
        [SerializeField] private ObjectiveTracker m_ObjectiveTracker;
        [SerializeField] private BallPicker m_BallPicker;
        [SerializeField] private UIManager m_UIManager;

        private int m_LevelIndex;
        private bool m_WinRecorded;

        public int LevelIndex => m_LevelIndex;
        public LevelPack LevelPack => m_LevelPack;
        public bool HasNextLevel => m_LevelPack != null && m_LevelPack.HasNext(m_LevelIndex);

        public UnityAction<LevelDefinition> OnLevelCompleted;

        public void SetReferences(LevelPack levelPack, LevelController levelController, ObjectiveTracker objectiveTracker, BallPicker ballPicker, UIManager uiManager)
        {
            m_LevelPack = levelPack;
            m_LevelController = levelController;
            m_ObjectiveTracker = objectiveTracker;
            m_BallPicker = ballPicker;
            m_UIManager = uiManager;
        }

        private void Awake()
        {
            if (m_LevelController == null) m_LevelController = FindFirstObjectByType<LevelController>();
            if (m_ObjectiveTracker == null) m_ObjectiveTracker = FindFirstObjectByType<ObjectiveTracker>();
            if (m_BallPicker == null) m_BallPicker = FindFirstObjectByType<BallPicker>();
            if (m_UIManager == null) m_UIManager = FindFirstObjectByType<UIManager>();

            SelectStartingLevel();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }

            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt += HandleLevelBuilt;

                // LevelController.Start may already have built the board
                if (m_LevelController.State != null && m_LevelController.CurrentLevel != null)
                {
                    HandleLevelBuilt(m_LevelController.CurrentLevel);
                }
            }

            UIWin win = GetWinPopup();
            if (win != null)
            {
                win.OnReplay += Replay;
                win.OnNext += NextLevel;
                win.OnMenu += GoToMenu;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }

            if (m_LevelController != null)
            {
                m_LevelController.OnLevelBuilt -= HandleLevelBuilt;
            }

            UIWin win = GetWinPopup();
            if (win != null)
            {
                win.OnReplay -= Replay;
                win.OnNext -= NextLevel;
                win.OnMenu -= GoToMenu;
            }
        }

        /// <summary>Resolves the saved level index against the pack before LevelController.Start builds.</summary>
        private void SelectStartingLevel()
        {
            if (m_LevelPack == null || m_LevelPack.Count == 0 || m_LevelController == null) return;

            var save = ServiceLocator.Get<ISaveService>();
            int savedIndex = save != null ? save.CurrentLevelIndex : 0;
            m_LevelIndex = m_LevelPack.ClampIndex(savedIndex);

            LevelDefinition level = m_LevelPack.Get(m_LevelIndex);
            if (level != null)
            {
                m_LevelController.SetLevel(level);
            }
        }

        private void HandleLevelBuilt(LevelDefinition levelDef)
        {
            m_WinRecorded = false;

            if (m_LevelPack != null)
            {
                int index = m_LevelPack.IndexOf(levelDef);
                if (index >= 0) m_LevelIndex = index;
            }

            SyncBallPicker(levelDef);

            // Restart / Replay / Next while the win popup is up: the new board takes over
            if (m_UIManager != null && m_UIManager.Current == PopupId.LevelComplete)
            {
                m_UIManager.Hide();
            }
        }

        private void SyncBallPicker(LevelDefinition levelDef)
        {
            if (m_BallPicker == null || levelDef == null) return;

            IReadOnlyList<GameColor> colors = levelDef.ColorsUsed;
            IReadOnlyList<GameColor> current = m_BallPicker.AvailableColors;

            bool same = current.Count == colors.Count;
            for (int i = 0; same && i < colors.Count; i++)
            {
                same = current[i] == colors[i];
            }

            // Same kinds → keep the loaded kind across restarts (gameplay.md §5)
            if (!same)
            {
                m_BallPicker.Build(colors);
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Won || m_WinRecorded) return;

            m_WinRecorded = true;
            LevelDefinition level = m_LevelController != null ? m_LevelController.CurrentLevel : null;
            int shots = m_ObjectiveTracker != null ? m_ObjectiveTracker.ShotsTaken : 0;

            RecordWin(level, shots, out int bestShots, out bool isNewBest);
            OnLevelCompleted?.Invoke(level);

            if (m_UIManager != null)
            {
                UIWin win = m_UIManager.Show<UIWin>(PopupId.LevelComplete);
                if (win != null)
                {
                    win.Bind(level != null ? level.DisplayName : null, shots, bestShots, isNewBest, HasNextLevel);
                }
            }
        }

        private void RecordWin(LevelDefinition level, int shots, out int bestShots, out bool isNewBest)
        {
            bestShots = shots;
            isNewBest = false;

            var save = ServiceLocator.Get<ISaveService>();
            if (save == null || save.Data == null || level == null) return;

            LevelRecord record = save.Data.GetOrCreateRecord(level.LevelId);
            isNewBest = record.RegisterCompletion(shots);
            bestShots = record.BestShots;

            // Resume from the next level; the last level of the pack stays replayable
            int nextIndex = HasNextLevel ? m_LevelIndex + 1 : m_LevelIndex;
            save.HighestUnlockedLevel = Mathf.Max(save.HighestUnlockedLevel, nextIndex);
            save.CurrentLevelIndex = nextIndex;
            save.Save();
        }

        public void Replay()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        public void NextLevel()
        {
            if (!HasNextLevel)
            {
                Replay();
                return;
            }

            m_LevelIndex++;
            var save = ServiceLocator.Get<ISaveService>();
            if (save != null)
            {
                save.CurrentLevelIndex = m_LevelIndex;
                save.Save();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadLevel(m_LevelPack.Get(m_LevelIndex));
            }
        }

        public void GoToMenu()
        {
            if (m_UIManager != null)
            {
                m_UIManager.Hide();
            }

            var loader = ServiceLocator.Get<ISceneLoader>();
            if (loader != null)
            {
                loader.Load(GameConstants.SceneMainMenu);
            }
            else
            {
                SceneManager.LoadScene(GameConstants.SceneMainMenu);
            }
        }

        private UIWin GetWinPopup()
        {
            return m_UIManager != null ? m_UIManager.Get<UIWin>(PopupId.LevelComplete) : null;
        }
    }
}
