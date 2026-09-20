using UnityEngine;

namespace Game.Runtime.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaRect : MonoBehaviour
    {
        private RectTransform m_RectTransform;
        private Rect m_LastSafeArea;
        private Vector2Int m_LastScreenSize;

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (m_LastSafeArea != Screen.safeArea ||
                m_LastScreenSize.x != Screen.width ||
                m_LastScreenSize.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (m_RectTransform == null)
            {
                m_RectTransform = GetComponent<RectTransform>();
            }

            Rect safeArea = Screen.safeArea;
            m_LastSafeArea = safeArea;
            m_LastScreenSize = new Vector2Int(Screen.width, Screen.height);

            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            m_RectTransform.anchorMin = anchorMin;
            m_RectTransform.anchorMax = anchorMax;
            m_RectTransform.offsetMin = Vector2.zero;
            m_RectTransform.offsetMax = Vector2.zero;
        }
    }
}
