using Game.Board;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    /// <summary>
    /// 01_MainMenu hub (scene_structure.md §1): title, the level Play resumes, a big Play button
    /// and settings. Shop / remove-ads is deliberately absent until the IAP milestone.
    /// </summary>
    public class UIMainMenu : UIBase
    {
        [SerializeField] private LevelPack m_LevelPack;
        [SerializeField] private UIManager m_UIManager;
        [SerializeField] private TMP_Text m_LevelText;
        [SerializeField] private Button m_PlayButton;
        [SerializeField] private Button m_SettingsButton;

        public void SetReferences(LevelPack levelPack, UIManager uiManager)
        {
            m_LevelPack = levelPack;
            m_UIManager = uiManager;
        }

        public void SetWidgets(TMP_Text levelText, Button playButton, Button settingsButton)
        {
            m_LevelText = levelText;
            m_PlayButton = playButton;
            m_SettingsButton = settingsButton;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_UIManager == null) m_UIManager = FindFirstObjectByType<UIManager>();
            if (m_PlayButton != null) m_PlayButton.onClick.AddListener(OnPlayClicked);
            if (m_SettingsButton != null) m_SettingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void Start()
        {
            Show(true);
        }

        protected override void OnShow()
        {
            if (m_PlayButton != null) m_PlayButton.interactable = true;
            RefreshLevelLabel();
        }

        public void RefreshLevelLabel()
        {
            if (m_LevelText == null) return;

            var save = ServiceLocator.Get<ISaveService>();
            int index = save != null ? save.CurrentLevelIndex : 0;

            LevelDefinition level = m_LevelPack != null ? m_LevelPack.Get(index) : null;
            m_LevelText.text = level != null && !string.IsNullOrEmpty(level.DisplayName)
                ? level.DisplayName
                : $"LEVEL {index + 1}";
        }

        private void OnPlayClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);

            var loader = ServiceLocator.Get<ISceneLoader>();
            if (loader == null)
            {
                Debug.LogError("[UIMainMenu] No ISceneLoader registered — cannot start gameplay.");
                return;
            }

            // Guard against a double tap queuing two loads
            if (m_PlayButton != null) m_PlayButton.interactable = false;
            loader.Load(GameConstants.SceneGameplay);
        }

        private void OnSettingsClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
            if (m_UIManager != null)
            {
                m_UIManager.Show(PopupId.Settings);
            }
        }
    }
}
