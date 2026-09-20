using System.Collections;
using Game.Core;
using Game.Runtime.Services;
using UnityEngine;

namespace Game.Runtime.Ball
{
    [RequireComponent(typeof(LineRenderer))]
    public class TrailRun : MonoBehaviour
    {
        [SerializeField] private LineRenderer m_LineRenderer;
        [SerializeField] private float m_StartWidth = 0.24f;
        [SerializeField] private float m_EndWidth = 0.08f;

        private Vector2 m_StartPoint;
        private Vector2 m_EndPoint;
        private Color m_BaseColor = Color.white;
        private Gradient m_BaseGradient;
        private float m_AgeWeight = 1.0f;
        private Coroutine m_FadeCoroutine;

        public LineRenderer Line => m_LineRenderer;
        public Vector2 StartPoint => m_StartPoint;
        public Vector2 EndPoint => m_EndPoint;

        private void Awake()
        {
            if (m_LineRenderer == null)
            {
                m_LineRenderer = GetComponent<LineRenderer>();
            }

            m_LineRenderer.positionCount = 2;
            m_LineRenderer.sortingLayerName = GameConstants.SortingBallTrail;
            m_LineRenderer.useWorldSpace = true;
        }

        public void Initialize(Vector2 startPos, Color color, Gradient gradient = null)
        {
            if (m_FadeCoroutine != null)
            {
                StopCoroutine(m_FadeCoroutine);
                m_FadeCoroutine = null;
            }

            if (m_LineRenderer == null)
            {
                m_LineRenderer = GetComponent<LineRenderer>();
            }

            m_StartPoint = startPos;
            m_EndPoint = startPos;
            m_BaseColor = color;
            m_BaseGradient = gradient;
            m_AgeWeight = 1.0f;

            m_LineRenderer.positionCount = 2;
            m_LineRenderer.SetPosition(0, new Vector3(startPos.x, startPos.y, 0f));
            m_LineRenderer.SetPosition(1, new Vector3(startPos.x, startPos.y, 0f));
            m_LineRenderer.startWidth = m_EndWidth;
            m_LineRenderer.endWidth = m_StartWidth;

            ApplyGradient(1.0f);
        }

        public void UpdateHead(Vector2 headPos)
        {
            m_EndPoint = headPos;
            m_LineRenderer.SetPosition(1, new Vector3(headPos.x, headPos.y, 0f));
        }

        public void SetAgeWeight(float weight)
        {
            m_AgeWeight = Mathf.Clamp01(weight);
            ApplyGradient(m_AgeWeight);
        }

        private void ApplyGradient(float alphaScale)
        {
            if (m_BaseGradient != null)
            {
                Gradient scaledGradient = new Gradient();
                var colorKeys = m_BaseGradient.colorKeys;
                var alphaKeys = m_BaseGradient.alphaKeys;
                var scaledAlphaKeys = new GradientAlphaKey[alphaKeys.Length];

                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    scaledAlphaKeys[i] = new GradientAlphaKey(alphaKeys[i].alpha * alphaScale, alphaKeys[i].time);
                }

                scaledGradient.SetKeys(colorKeys, scaledAlphaKeys);
                m_LineRenderer.colorGradient = scaledGradient;
            }
            else
            {
                // Default comet gradient: tail (point 0) is faint, head (point 1, at ball) is bright
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(m_BaseColor, 0.0f),
                        new GradientColorKey(m_BaseColor, 1.0f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.05f * alphaScale, 0.0f),
                        new GradientAlphaKey(0.85f * alphaScale, 1.0f)
                    }
                );
                m_LineRenderer.colorGradient = grad;
            }
        }

        public void FadeAndDespawn(float duration, PoolService poolService)
        {
            if (m_FadeCoroutine != null)
            {
                StopCoroutine(m_FadeCoroutine);
            }
            m_FadeCoroutine = StartCoroutine(FadeRoutine(duration, poolService));
        }

        public void Dissipate(float duration, PoolService poolService)
        {
            FadeAndDespawn(duration, poolService);
        }

        private IEnumerator FadeRoutine(float duration, PoolService poolService)
        {
            float elapsed = 0f;
            float startWeight = m_AgeWeight;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentWeight = Mathf.Lerp(startWeight, 0f, t);
                ApplyGradient(currentWeight);
                yield return null;
            }

            ApplyGradient(0f);
            m_FadeCoroutine = null;

            if (poolService != null)
            {
                poolService.Despawn(gameObject);
            }
            else if (PoolService.HasInstance)
            {
                PoolService.Instance.Despawn(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
