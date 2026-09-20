using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Core
{
    /// <summary>
    /// Base for every screen/popup: CanvasGroup fade + panel scale (technical_design.md §6.4).
    /// Show: scale 0.9→1 + alpha 0→1, 0.15 s, OutBack. Hide: reverse, 0.12 s, InBack.
    /// Runs on unscaled time so it keeps animating while the game is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIBase : MonoBehaviour
    {
        private const float ShowDuration = 0.15f;
        private const float HideDuration = 0.12f;
        private const float HiddenScale = 0.9f;

        [SerializeField] private CanvasGroup m_CanvasGroup;
        [Tooltip("Scaled during show/hide. Leave empty to only fade.")]
        [SerializeField] private RectTransform m_AnimatedPanel;

        private Coroutine m_TransitionRoutine;
        private bool m_IsVisible;

        public bool IsVisible => m_IsVisible;
        public CanvasGroup CanvasGroup => m_CanvasGroup;

        public UnityAction OnShown;
        public UnityAction OnHidden;

        protected virtual void Awake()
        {
            EnsureCanvasGroup();
        }

        public void SetAnimatedPanel(RectTransform panel)
        {
            m_AnimatedPanel = panel;
        }

        public void Show(bool instant = false)
        {
            EnsureCanvasGroup();
            gameObject.SetActive(true);
            m_IsVisible = true;
            StopTransition();

            m_CanvasGroup.interactable = true;
            m_CanvasGroup.blocksRaycasts = true;

            OnShow();

            if (instant || !isActiveAndEnabled)
            {
                ApplyVisual(1f, 1f);
                OnShown?.Invoke();
                return;
            }

            m_TransitionRoutine = StartCoroutine(TransitionRoutine(0f, 1f, HiddenScale, 1f, ShowDuration, true));
        }

        public void Hide(bool instant = false)
        {
            EnsureCanvasGroup();
            bool wasVisible = m_IsVisible;
            m_IsVisible = false;
            StopTransition();

            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;

            if (wasVisible)
            {
                OnHide();
            }

            if (instant || !isActiveAndEnabled)
            {
                ApplyVisual(0f, HiddenScale);
                gameObject.SetActive(false);
                if (wasVisible) OnHidden?.Invoke();
                return;
            }

            m_TransitionRoutine = StartCoroutine(TransitionRoutine(m_CanvasGroup.alpha, 0f, 1f, HiddenScale, HideDuration, false));
        }

        protected virtual void OnShow() { }

        protected virtual void OnHide() { }

        private void EnsureCanvasGroup()
        {
            if (m_CanvasGroup == null)
            {
                m_CanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void StopTransition()
        {
            if (m_TransitionRoutine != null)
            {
                StopCoroutine(m_TransitionRoutine);
                m_TransitionRoutine = null;
            }
        }

        private void ApplyVisual(float alpha, float scale)
        {
            m_CanvasGroup.alpha = alpha;
            if (m_AnimatedPanel != null)
            {
                m_AnimatedPanel.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private IEnumerator TransitionRoutine(float fromAlpha, float toAlpha, float fromScale, float toScale, float duration, bool showing)
        {
            float elapsed = 0f;
            ApplyVisual(fromAlpha, fromScale);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = showing ? EaseOutBack(t) : EaseInBack(t);
                ApplyVisual(Mathf.Lerp(fromAlpha, toAlpha, t), Mathf.LerpUnclamped(fromScale, toScale, eased));
                yield return null;
            }

            ApplyVisual(toAlpha, toScale);
            m_TransitionRoutine = null;

            if (showing)
            {
                OnShown?.Invoke();
            }
            else
            {
                gameObject.SetActive(false);
                OnHidden?.Invoke();
            }
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float p = t - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        private static float EaseInBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return c3 * t * t * t - c1 * t * t;
        }
    }
}
