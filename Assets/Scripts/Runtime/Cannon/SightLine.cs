using Game.Core;
using UnityEngine;

namespace Game.Runtime.Cannon
{
    [RequireComponent(typeof(LineRenderer))]
    public class SightLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer m_LineRenderer;
        [SerializeField] private Color m_LineColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private float m_StartWidth = 0.04f;
        [SerializeField] private float m_EndWidth = 0.08f;

        private void Awake()
        {
            EnsureLineRenderer();
        }

        private void EnsureLineRenderer()
        {
            if (m_LineRenderer == null)
            {
                m_LineRenderer = GetComponent<LineRenderer>();
            }

            if (m_LineRenderer != null)
            {
                m_LineRenderer.positionCount = 2;
                m_LineRenderer.useWorldSpace = true;
                m_LineRenderer.sortingLayerName = GameConstants.SortingAimGuide;
                m_LineRenderer.startWidth = m_StartWidth;
                m_LineRenderer.endWidth = m_EndWidth;
                m_LineRenderer.startColor = m_LineColor;
                m_LineRenderer.endColor = m_LineColor;
            }
        }

        public void Set(Vector2 from, Vector2 to)
        {
            EnsureLineRenderer();
            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = true;
                m_LineRenderer.SetPosition(0, new Vector3(from.x, from.y, 0f));
                m_LineRenderer.SetPosition(1, new Vector3(to.x, to.y, 0f));
            }
        }

        public void SetVisible(bool visible)
        {
            EnsureLineRenderer();
            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = visible;
            }
        }
    }
}
