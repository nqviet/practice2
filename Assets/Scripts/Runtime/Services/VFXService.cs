using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Runtime.Services
{
    public class VFXService : Singleton<VFXService>, IVFXService
    {
        [SerializeField] private PoolService m_PoolService;
        [SerializeField] private Sprite m_RadialFlashSprite;

        private int m_ActiveSettleTokens;
        public int ActiveSettleTokens => m_ActiveSettleTokens;
        public bool IsSettling => m_ActiveSettleTokens > 0;

        public UnityAction OnSettleCompleted;

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register<IVFXService>(this);
            if (m_PoolService == null)
            {
                m_PoolService = PoolService.Instance;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<IVFXService>();
        }

        public void SetReferences(PoolService poolService, Sprite radialFlashSprite = null)
        {
            m_PoolService = poolService;
            m_RadialFlashSprite = radialFlashSprite;
        }

        public void RetainSettleToken()
        {
            m_ActiveSettleTokens++;
        }

        public void ReleaseSettleToken()
        {
            m_ActiveSettleTokens = Mathf.Max(0, m_ActiveSettleTokens - 1);
            if (m_ActiveSettleTokens == 0)
            {
                OnSettleCompleted?.Invoke();
            }
        }

        public void PlayBreak(Vector2 worldPos, GameColor color, int depth = 0)
        {
            PoolId poolId;
            Color flashColor;

            switch (color)
            {
                case GameColor.Red:
                    poolId = PoolId.BreakFxRed;
                    flashColor = new Color(1f, 0.25f, 0.25f, 0.85f);
                    break;
                case GameColor.Blue:
                    poolId = PoolId.BreakFxBlue;
                    flashColor = new Color(0.25f, 0.55f, 1f, 0.85f);
                    break;
                case GameColor.Yellow:
                    poolId = PoolId.BreakFxYellow;
                    flashColor = new Color(1f, 0.85f, 0.2f, 0.85f);
                    break;
                default:
                    poolId = PoolId.BreakFxNeutral;
                    flashColor = new Color(0.9f, 0.9f, 0.95f, 0.6f);
                    break;
            }

            if (m_PoolService == null) m_PoolService = PoolService.Instance;

            // Spawn particle burst
            if (m_PoolService != null)
            {
                GameObject fxGo = m_PoolService.Spawn(poolId, worldPos, Quaternion.identity);
                if (fxGo != null)
                {
                    var ps = fxGo.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        ps.Clear();
                        ps.Play();
                    }

                    RetainSettleToken();
                    StartCoroutine(AutoDespawnRoutine(fxGo, 0.55f, true));
                }
            }

            // Additive radial flash (§7.3: scale 0.3 -> 1.4 over 0.25s)
            PlayRadialFlash(worldPos, flashColor, 0.3f, 1.4f, 0.25f);
        }

        public void PlayImpact(Vector2 worldPos, float intensity = 1.0f)
        {
            if (m_PoolService == null) m_PoolService = PoolService.Instance;
            if (m_PoolService == null) return;

            GameObject fxGo = m_PoolService.Spawn(PoolId.ImpactSpark, worldPos, Quaternion.identity);
            if (fxGo != null)
            {
                var ps = fxGo.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }

                StartCoroutine(AutoDespawnRoutine(fxGo, 0.20f, false));
            }
        }

        public void PlayMuzzleFlash(Vector2 worldPos, Vector2 direction)
        {
            if (m_PoolService == null) m_PoolService = PoolService.Instance;
            if (m_PoolService == null) return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            Quaternion rot = Quaternion.Euler(0f, 0f, angle);

            GameObject fxGo = m_PoolService.Spawn(PoolId.MuzzleFlash, worldPos, rot);
            if (fxGo != null)
            {
                var ps = fxGo.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }

                StartCoroutine(AutoDespawnRoutine(fxGo, 0.18f, false));
            }
        }

        public void PlayRadialFlash(Vector2 worldPos, Color color, float startScale = 0.3f, float endScale = 1.4f, float duration = 0.25f)
        {
            StartCoroutine(RadialFlashRoutine(worldPos, color, startScale, endScale, duration));
        }

        private IEnumerator AutoDespawnRoutine(GameObject fxGo, float lifetime, bool isSettleToken)
        {
            yield return new WaitForSeconds(lifetime);

            if (fxGo != null && m_PoolService != null)
            {
                m_PoolService.Despawn(fxGo);
            }

            if (isSettleToken)
            {
                ReleaseSettleToken();
            }
        }

        private IEnumerator RadialFlashRoutine(Vector2 worldPos, Color color, float startScale, float endScale, float duration)
        {
            var flashGo = new GameObject("RadialFlashInstance");
            flashGo.transform.position = worldPos;
            flashGo.transform.localScale = Vector3.one * startScale;

            var sr = flashGo.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = GameConstants.SortingFX;
            if (m_RadialFlashSprite != null)
            {
                sr.sprite = m_RadialFlashSprite;
            }
            sr.color = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale = Mathf.Lerp(startScale, endScale, t);
                flashGo.transform.localScale = Vector3.one * scale;

                Color c = color;
                c.a = Mathf.Lerp(color.a, 0f, t);
                sr.color = c;

                yield return null;
            }

            Destroy(flashGo);
        }
    }
}
