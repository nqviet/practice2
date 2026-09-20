using Game.Core;
using Game.Utils;
using UnityEngine;

namespace Game.Runtime.Services
{
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        [SerializeField] private Camera m_Camera;
        [SerializeField] private Transform m_CannonTransform;
        [SerializeField] private GameplayConfig m_Config;
        [SerializeField] private AdController m_AdController;

        private float m_OrthoSize;
        private float m_CameraY;
        private float m_CannonY;
        private int m_LastScreenWidth;
        private int m_LastScreenHeight;

        public float OrthoSize => m_OrthoSize;
        public float CameraY => m_CameraY;
        public float CannonY => m_CannonY;

        public void SetReferences(Camera cam, Transform cannonTransform, GameplayConfig config, AdController adController)
        {
            m_Camera = cam;
            m_CannonTransform = cannonTransform;
            m_Config = config;
            m_AdController = adController;

            if (m_AdController != null)
            {
                m_AdController.OnBannerReserveChanged -= HandleBannerReserveChanged;
                m_AdController.OnBannerReserveChanged += HandleBannerReserveChanged;
            }
        }

        private void Awake()
        {
            if (m_Camera == null)
            {
                m_Camera = GetComponent<Camera>();
            }
            if (m_AdController == null)
            {
                m_AdController = FindFirstObjectByType<AdController>();
            }
        }

        private void OnEnable()
        {
            if (m_AdController != null)
            {
                m_AdController.OnBannerReserveChanged += HandleBannerReserveChanged;
            }
        }

        private void OnDisable()
        {
            if (m_AdController != null)
            {
                m_AdController.OnBannerReserveChanged -= HandleBannerReserveChanged;
            }
        }

        private void Start()
        {
            Refit();
        }

        private void Update()
        {
            if (Screen.width != m_LastScreenWidth || Screen.height != m_LastScreenHeight)
            {
                Refit();
            }
        }

        public void Refit()
        {
            m_LastScreenWidth = Screen.width;
            m_LastScreenHeight = Screen.height;

            float aspect = m_Camera != null && m_Camera.aspect > 0f
                ? m_Camera.aspect
                : (Screen.width > 0 && Screen.height > 0 ? (float)Screen.width / Screen.height : 9f / 16f);

            float bannerUnits = m_AdController != null ? m_AdController.BannerUnits : 0f;
            Refit(aspect, bannerUnits);
        }

        public void Refit(float aspect, float bannerUnits)
        {
            if (m_Camera == null)
            {
                m_Camera = GetComponent<Camera>();
            }

            m_OrthoSize = CameraMath.CalculateOrthoSize(aspect);
            m_CameraY = CameraMath.CalculateCameraY(m_OrthoSize);

            if (m_Camera != null)
            {
                m_Camera.orthographic = true;
                m_Camera.orthographicSize = m_OrthoSize;
                Vector3 camPos = m_Camera.transform.position;
                camPos.x = 0f;
                camPos.y = m_CameraY;
                m_Camera.transform.position = camPos;
            }

            float screenBottom = CameraMath.CalculateScreenBottom(m_CameraY, m_OrthoSize);
            float pad = m_Config != null ? m_Config.CannonBottomPad : 1.8f;
            m_CannonY = CameraMath.CalculateCannonY(screenBottom, bannerUnits, pad);

            if (m_CannonTransform != null)
            {
                Vector3 cannonPos = m_CannonTransform.position;
                cannonPos.y = m_CannonY;
                m_CannonTransform.position = cannonPos;
            }
        }

        private void HandleBannerReserveChanged(float bannerUnits)
        {
            Refit();
        }
    }
}
