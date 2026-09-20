using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    /// <summary>
    /// Popup_LevelComplete (gameplay.md §7): "Level Complete", the shot count as information
    /// only (§9 decision 4 — no stars, no rewards), then Replay / Next / Menu.
    /// There is no fail counterpart: the level cannot be lost.
    /// </summary>
    public class UIWin : UIPopup
    {
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_ShotsText;
        [SerializeField] private TMP_Text m_BestText;
        [SerializeField] private Button m_ReplayButton;
        [SerializeField] private Button m_NextButton;
        [SerializeField] private Button m_MenuButton;

        public UnityAction OnReplay;
        public UnityAction OnNext;
        public UnityAction OnMenu;

        public void SetWidgets(TMP_Text titleText, TMP_Text shotsText, TMP_Text bestText, Button replayButton, Button nextButton, Button menuButton)
        {
            m_TitleText = titleText;
            m_ShotsText = shotsText;
            m_BestText = bestText;
            m_ReplayButton = replayButton;
            m_NextButton = nextButton;
            m_MenuButton = menuButton;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_ReplayButton != null) m_ReplayButton.onClick.AddListener(OnReplayClicked);
            if (m_NextButton != null) m_NextButton.onClick.AddListener(OnNextClicked);
            if (m_MenuButton != null) m_MenuButton.onClick.AddListener(OnMenuClicked);
        }

        public void Bind(string levelName, int shots, int bestShots, bool isNewBest, bool hasNextLevel)
        {
            if (m_TitleText != null)
            {
                m_TitleText.text = string.IsNullOrEmpty(levelName) ? "LEVEL COMPLETE" : $"{levelName}\nCOMPLETE";
            }

            if (m_ShotsText != null)
            {
                m_ShotsText.text = shots == 1 ? "Cleared in 1 shot" : $"Cleared in {shots} shots";
            }

            if (m_BestText != null)
            {
                m_BestText.text = isNewBest ? "New best!" : $"Best: {bestShots}";
            }

            if (m_NextButton != null)
            {
                m_NextButton.gameObject.SetActive(hasNextLevel);
            }
        }

        private void OnReplayClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
            OnReplay?.Invoke();
        }

        private void OnNextClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
            OnNext?.Invoke();
        }

        private void OnMenuClicked()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);
            OnMenu?.Invoke();
        }
    }
}
