using Game.Runtime.Services;
using UnityEngine;

namespace Game.Runtime.UI
{
    /// <summary>
    /// Lifts a bottom-anchored RectTransform by the ad banner reserve (technical_design.md §6.2),
    /// so the BottomBar tray always sits directly above the banner and directly below the cannon.
    /// AdController owns BannerUnits (world units); this converts them to canvas units.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class BannerInset : MonoBehaviour
    {
        [SerializeField] private AdController m_AdController;
        [SerializeField] private Camera m_Camera;

        private RectTransform m_RectTransform;
        private RectTransform m_CanvasRect;
        private float m_LastInset = -1f;

        public void SetReferences(AdController adController, Camera cam)
        {
            m_AdController = adController;
            m_Camera = cam;
        }

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            if (m_AdController == null) m_AdController = FindFirstObjectByType<AdController>();
            if (m_Camera == null) m_Camera = Camera.main;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) m_CanvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        }

        // LateUpdate: runs after CameraFitter has refit the ortho size for this frame
        private void LateUpdate()
        {
            if (m_Camera == null || m_CanvasRect == null) return;

            float bannerUnits = m_AdController != null ? m_AdController.BannerUnits : 0f;
            float canvasPerUnit = m_CanvasRect.rect.height / (2f * m_Camera.orthographicSize);
            float inset = bannerUnits * canvasPerUnit;

            if (Mathf.Abs(inset - m_LastInset) < 0.5f) return;
            m_LastInset = inset;

            Vector2 position = m_RectTransform.anchoredPosition;
            position.y = inset;
            m_RectTransform.anchoredPosition = position;
        }
    }
}
