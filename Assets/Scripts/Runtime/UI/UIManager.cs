using System.Collections.Generic;
using Game.Core;
using Game.Runtime.GameFlow;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Game.Runtime.UI
{
    /// <summary>
    /// Popup host on Canvas_Popups/PopupHost — one popup at a time (technical_design.md §2, §6.1).
    /// One per scene; the newest instance wins, nothing is destroyed on conflict.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager s_Instance;

        [SerializeField] private List<UIPopup> m_Popups = new List<UIPopup>();

        private UIPopup m_Current;
        private bool m_PausedByPopup;

        public static UIManager Instance => s_Instance;
        public PopupId Current => m_Current != null ? m_Current.PopupId : PopupId.None;
        public bool IsPopupOpen => m_Current != null;

        public UnityAction<PopupId> OnPopupShown;
        public UnityAction OnPopupClosed;

        public void SetPopups(IEnumerable<UIPopup> popups)
        {
            m_Popups = popups != null ? new List<UIPopup>(popups) : new List<UIPopup>();
        }

        private void Awake()
        {
            s_Instance = this;

            if (m_Popups.Count == 0)
            {
                m_Popups.AddRange(GetComponentsInChildren<UIPopup>(true));
            }

            for (int i = 0; i < m_Popups.Count; i++)
            {
                if (m_Popups[i] != null)
                {
                    m_Popups[i].Hide(true);
                }
            }
        }

        private void OnDestroy()
        {
            if (m_PausedByPopup && GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(false);
            }

            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        private void Update()
        {
            // Android back button maps to Escape
            if (m_Current != null && m_Current.CloseOnBack &&
                Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
            }
        }

        public UIPopup Get(PopupId id)
        {
            for (int i = 0; i < m_Popups.Count; i++)
            {
                if (m_Popups[i] != null && m_Popups[i].PopupId == id)
                {
                    return m_Popups[i];
                }
            }
            return null;
        }

        public T Get<T>(PopupId id) where T : UIPopup
        {
            return Get(id) as T;
        }

        public UIPopup Show(PopupId id)
        {
            UIPopup popup = Get(id);
            if (popup == null)
            {
                Debug.LogWarning($"[UIManager] No popup registered for '{id}'.");
                return null;
            }

            if (m_Current == popup)
            {
                return popup;
            }

            if (m_Current != null)
            {
                CloseCurrent(true);
            }

            m_Current = popup;
            popup.transform.SetAsLastSibling();
            popup.Show();

            if (popup.PausesGame && GameManager.Instance != null && !GameManager.Instance.IsPaused)
            {
                GameManager.Instance.SetPaused(true);
                m_PausedByPopup = true;
            }

            OnPopupShown?.Invoke(id);
            return popup;
        }

        public T Show<T>(PopupId id) where T : UIPopup
        {
            return Show(id) as T;
        }

        public void Hide()
        {
            if (m_Current == null) return;

            CloseCurrent(false);
            OnPopupClosed?.Invoke();
        }

        private void CloseCurrent(bool instant)
        {
            UIPopup closing = m_Current;
            m_Current = null;
            closing.Hide(instant);

            if (m_PausedByPopup)
            {
                m_PausedByPopup = false;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPaused(false);
                }
            }
        }
    }
}
