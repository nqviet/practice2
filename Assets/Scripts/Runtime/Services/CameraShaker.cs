using System.Collections;
using Game.Core;
using Game.Utils;
using UnityEngine;
using Unity.Cinemachine;

namespace Game.Runtime.Services
{
    public class CameraShaker : Singleton<CameraShaker>, ICameraShaker
    {
        [SerializeField] private GameplayConfig m_Config;
        [SerializeField] private Camera m_TargetCamera;
        [SerializeField] private CinemachineImpulseSource m_ImpulseSource;

        private Coroutine m_FallbackShakeCoroutine;
        private Vector3 m_OriginalCameraLocalPos;

        public void SetReferences(GameplayConfig config, Camera targetCamera)
        {
            m_Config = config;
            m_TargetCamera = targetCamera;

            if (m_ImpulseSource == null)
            {
                m_ImpulseSource = GetComponent<CinemachineImpulseSource>();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register<ICameraShaker>(this);

            if (m_TargetCamera == null)
            {
                m_TargetCamera = Camera.main;
            }

            if (m_TargetCamera != null)
            {
                m_OriginalCameraLocalPos = m_TargetCamera.transform.localPosition;
            }

            if (m_ImpulseSource == null)
            {
                m_ImpulseSource = GetComponent<CinemachineImpulseSource>();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<ICameraShaker>();
        }

        public void Shake(float amplitude = 1.0f)
        {
            float scale = m_Config != null ? m_Config.ShakeScale : 1.0f;
            // Impulse capped at 0.25 per event (§8, Appendix §12)
            float finalAmplitude = Mathf.Clamp(amplitude * scale, 0.05f, 0.25f);

            bool impulseGenerated = false;

            if (m_ImpulseSource != null)
            {
                m_ImpulseSource.GenerateImpulse(finalAmplitude);
                impulseGenerated = true;
            }

            // Pause-safe fallback camera shake if impulse source not bound
            if (!impulseGenerated && m_TargetCamera != null)
            {
                if (m_FallbackShakeCoroutine != null)
                {
                    StopCoroutine(m_FallbackShakeCoroutine);
                }
                m_FallbackShakeCoroutine = StartCoroutine(FallbackShakeRoutine(finalAmplitude));
            }
        }

        private IEnumerator FallbackShakeRoutine(float amplitude)
        {
            const float duration = 0.15f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // Pause-safe: uses unscaledDeltaTime (§5.1)
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;
                float damper = 1.0f - Mathf.Clamp01(progress);

                float offsetX = (Random.value * 2f - 1f) * amplitude * damper;
                float offsetY = (Random.value * 2f - 1f) * amplitude * damper;

                m_TargetCamera.transform.localPosition = m_OriginalCameraLocalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            m_TargetCamera.transform.localPosition = m_OriginalCameraLocalPos;
            m_FallbackShakeCoroutine = null;
        }
    }
}
