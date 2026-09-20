using System.Collections;
using Game.Core;
using Game.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime.UI
{
    [RequireComponent(typeof(Image))]
    public class ScreenFeedback : Singleton<ScreenFeedback>, IScreenFeedback
    {
        [SerializeField] private Image m_OverlayImage;

        private Coroutine m_CurrentRoutine;

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register<IScreenFeedback>(this);

            if (m_OverlayImage == null)
            {
                m_OverlayImage = GetComponent<Image>();
            }

            if (m_OverlayImage != null)
            {
                m_OverlayImage.raycastTarget = false;
                m_OverlayImage.color = Color.clear;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<IScreenFeedback>();
        }

        public void Flash(Color color, float duration = 0.2f)
        {
            if (m_OverlayImage == null) return;

            if (m_CurrentRoutine != null)
            {
                StopCoroutine(m_CurrentRoutine);
            }
            m_CurrentRoutine = StartCoroutine(FlashRoutine(color, duration));
        }

        public void PulseVignette(Color color, float duration = 0.5f)
        {
            if (m_OverlayImage == null) return;

            if (m_CurrentRoutine != null)
            {
                StopCoroutine(m_CurrentRoutine);
            }
            m_CurrentRoutine = StartCoroutine(PulseRoutine(color, duration));
        }

        private IEnumerator FlashRoutine(Color targetColor, float duration)
        {
            float elapsed = 0f;
            m_OverlayImage.color = targetColor;

            while (elapsed < duration)
            {
                // Pause-safe: uses unscaledDeltaTime
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                m_OverlayImage.color = Color.Lerp(targetColor, Color.clear, t);
                yield return null;
            }

            m_OverlayImage.color = Color.clear;
            m_CurrentRoutine = null;
        }

        private IEnumerator PulseRoutine(Color pulseColor, float duration)
        {
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            // Fade in
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                m_OverlayImage.color = Color.Lerp(Color.clear, pulseColor, t);
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                m_OverlayImage.color = Color.Lerp(pulseColor, Color.clear, t);
                yield return null;
            }

            m_OverlayImage.color = Color.clear;
            m_CurrentRoutine = null;
        }
    }
}
