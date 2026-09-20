using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    /// <summary>
    /// A popup hosted by UIManager (Popup_Settings, Popup_LevelComplete, Popup_Shop).
    /// The root is a full-screen dimmer that blocks raycasts; the animated panel sits inside it.
    /// </summary>
    public class UIPopup : UIBase
    {
        [SerializeField] private PopupId m_PopupId = PopupId.None;
        [Tooltip("Freezes gameplay (GameManager pause gate) while open")]
        [SerializeField] private bool m_PausesGame;
        [Tooltip("Hardware back / Escape closes this popup")]
        [SerializeField] private bool m_CloseOnBack = true;
        [SerializeField] private Button m_CloseButton;

        public PopupId PopupId => m_PopupId;
        public bool PausesGame => m_PausesGame;
        public bool CloseOnBack => m_CloseOnBack;

        public void Configure(PopupId popupId, bool pausesGame, bool closeOnBack, Button closeButton)
        {
            m_PopupId = popupId;
            m_PausesGame = pausesGame;
            m_CloseOnBack = closeOnBack;
            m_CloseButton = closeButton;
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(OnCloseClicked);
            }
        }

        protected void Close()
        {
            ServiceLocator.Get<IAudioService>()?.Play(SfxId.UIClick);

            UIManager manager = UIManager.Instance;
            if (manager != null && manager.Current == m_PopupId)
            {
                manager.Hide();
            }
            else
            {
                Hide();
            }
        }

        private void OnCloseClicked()
        {
            Close();
        }
    }
}
