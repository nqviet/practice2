using Game.Core;
using Game.Runtime.Cannon;
using Game.Runtime.GameFlow;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    [RequireComponent(typeof(Button))]
    public class FireButton : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private InputReader m_InputReader;

        private void Awake()
        {
            if (m_Button == null) m_Button = GetComponent<Button>();
            if (m_InputReader == null) m_InputReader = FindFirstObjectByType<InputReader>();

            if (m_Button != null)
            {
                m_Button.onClick.AddListener(OnClick);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                UpdateInteractable(GameManager.Instance.State);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void OnClick()
        {
            if (m_InputReader != null)
            {
                m_InputReader.TriggerFire();
            }
        }

        private void HandleStateChanged(GameState state)
        {
            UpdateInteractable(state);
        }

        private void UpdateInteractable(GameState state)
        {
            if (m_Button == null) return;
            m_Button.interactable = (state == GameState.Ready || state == GameState.Aiming);
        }
    }
}
