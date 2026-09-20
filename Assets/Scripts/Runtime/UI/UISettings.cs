using Game.Core;
using Game.Runtime.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    /// <summary>
    /// Popup_Settings — the pause menu (gameplay.md §7): music / sound switches persisted via
    /// ISaveService, plus the same one-tap restart as the header and a way home. Restart / Home
    /// are hidden in the main menu, where there is nothing to restart.
    /// </summary>
    public class UISettings : UIPopup
    {
        [Header("Audio")]
        [SerializeField] private Button m_MusicButton;
        [SerializeField] private TMP_Text m_MusicLabel;
        [SerializeField] private Button m_SoundButton;
        [SerializeField] private TMP_Text m_SoundLabel;

        [Header("Gameplay Actions")]
        [SerializeField] private Button m_RestartButton;
        [SerializeField] private Button m_HomeButton;

        public void SetWidgets(Button musicButton, TMP_Text musicLabel, Button soundButton, TMP_Text soundLabel, Button restartButton, Button homeButton)
        {
            m_MusicButton = musicButton;
            m_MusicLabel = musicLabel;
            m_SoundButton = soundButton;
            m_SoundLabel = soundLabel;
            m_RestartButton = restartButton;
            m_HomeButton = homeButton;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_MusicButton != null) m_MusicButton.onClick.AddListener(OnMusicClicked);
            if (m_SoundButton != null) m_SoundButton.onClick.AddListener(OnSoundClicked);
            if (m_RestartButton != null) m_RestartButton.onClick.AddListener(OnRestartClicked);
            if (m_HomeButton != null) m_HomeButton.onClick.AddListener(OnHomeClicked);
        }

        protected override void OnShow()
        {
            bool inGameplay = GameManager.Instance != null;
            if (m_RestartButton != null) m_RestartButton.gameObject.SetActive(inGameplay);
            if (m_HomeButton != null) m_HomeButton.gameObject.SetActive(inGameplay);

            RefreshLabels();
        }

        private void OnMusicClicked()
        {
            PlayerData data = GetData();
            if (data == null) return;

            data.MusicEnabled = !data.MusicEnabled;
            ServiceLocator.Get<IAudioService>()?.SetMusicVolume(data.EffectiveMusicVolume);
            Persist();
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
        }

        private void OnSoundClicked()
        {
            PlayerData data = GetData();
            if (data == null) return;

            data.SfxEnabled = !data.SfxEnabled;
            ServiceLocator.Get<IAudioService>()?.SetSfxVolume(data.EffectiveSfxVolume);
            Persist();
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
        }

        private void OnRestartClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.Restart);
            Close();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }

        private void OnHomeClicked()
        {
            Close();

            var loader = ServiceLocator.Get<ISceneLoader>();
            if (loader != null)
            {
                loader.Load(GameConstants.SceneMainMenu);
            }
        }

        private void Persist()
        {
            ServiceLocator.Get<ISaveService>()?.Save();
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            PlayerData data = GetData();
            bool musicOn = data == null || data.MusicEnabled;
            bool soundOn = data == null || data.SfxEnabled;

            if (m_MusicLabel != null) m_MusicLabel.text = musicOn ? "MUSIC: ON" : "MUSIC: OFF";
            if (m_SoundLabel != null) m_SoundLabel.text = soundOn ? "SOUND: ON" : "SOUND: OFF";
        }

        private static PlayerData GetData()
        {
            return ServiceLocator.Get<ISaveService>()?.Data;
        }
    }
}
