using System;
using Game.Core;
using UnityEngine;

namespace Game.Runtime.Services
{
    public class AdController : MonoBehaviour, IAdService
    {
        [SerializeField] private float m_BannerHeightDp = 50f;
        [SerializeField] private bool m_AdsRemoved = false;
        [SerializeField] private float m_BannerUnits;

        public event Action<float> OnBannerReserveChanged;

        public float BannerUnits => m_BannerUnits;
        public bool IsInterstitialReady => true;

        private void Awake()
        {
            RecalculateBannerUnits();
        }

        public void RecalculateBannerUnits(float orthoSize = 9.6f)
        {
            if (m_AdsRemoved)
            {
                SetBannerUnits(0f);
                return;
            }

            float screenHeight = Screen.height > 0 ? Screen.height : 1920f;
            float dpi = Screen.dpi > 0 ? Screen.dpi : 160f * (screenHeight / 1920f);
            float bannerPx = m_BannerHeightDp * (dpi / 160f);
            float unitsPerPx = (2f * orthoSize) / screenHeight;
            float computedUnits = bannerPx * unitsPerPx;

            SetBannerUnits(computedUnits);
        }

        public void SetBannerUnits(float units)
        {
            if (Mathf.Abs(m_BannerUnits - units) > 1e-4f)
            {
                m_BannerUnits = units;
                OnBannerReserveChanged?.Invoke(m_BannerUnits);
            }
        }

        public void SetAdsRemoved(bool removed)
        {
            m_AdsRemoved = removed;
            RecalculateBannerUnits();
        }

        public void ShowBanner()
        {
            if (m_AdsRemoved) return;
            RecalculateBannerUnits();
        }

        public void HideBanner()
        {
            SetBannerUnits(0f);
        }

        public void ShowInterstitial(Action onClosed)
        {
            onClosed?.Invoke();
        }
    }
}
