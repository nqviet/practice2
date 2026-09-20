using Game.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Runtime.Cannon
{
    public class AimController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameplayConfig m_Config;
        [SerializeField] private CannonController m_CannonController;
        [SerializeField] private SightLine m_SightLine;
        [SerializeField] private Transform m_AimReticle;

        [Header("Reticle Feedback")]
        [SerializeField] private float m_NormalScale = 1.0f;
        [SerializeField] private float m_HoldingScale = 1.25f;

        private Vector2 m_AimDirection = Vector2.up;
        private Vector2 m_CurrentReticlePos;
        private bool m_IsActive = true;
        private bool m_IsHolding = false;

        public Vector2 AimDirection => m_AimDirection;
        public Vector2 ReticlePosition => m_CurrentReticlePos;
        public Transform AimReticle => m_AimReticle;
        public UnityAction<Vector2> OnAimChanged;

        public void SetReferences(
            GameplayConfig config,
            CannonController cannonController,
            SightLine sightLine,
            Transform aimReticle)
        {
            m_Config = config;
            m_CannonController = cannonController;
            m_SightLine = sightLine;
            m_AimReticle = aimReticle;
        }

        private void Start()
        {
            // Initialize aim pointing straight up
            Vector2 defaultTarget = m_CannonController != null && m_CannonController.MuzzlePoint != null
                ? (Vector2)m_CannonController.MuzzlePoint.position + Vector2.up * 4.0f
                : Vector2.up * 4.0f;

            SetAimTarget(defaultTarget);
        }

        public void SetAimTarget(Vector2 worldTarget)
        {
            if (!m_IsActive) return;

            // Aim from the head's pivot: the muzzle swings around it, so only a ray from the pivot
            // stays on the path the ball (spawned at the rotated muzzle) actually travels
            Vector2 pivot = GetAimPivot();

            Vector2 dir = worldTarget - pivot;
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = Vector2.up;
            }

            // Clamp angle from Vector2.up
            float maxAngle = m_Config != null ? m_Config.AimClampFromUpDeg : 75.0f;
            float angle = Vector2.SignedAngle(Vector2.up, dir);
            float clampedAngle = Mathf.Clamp(angle, -maxAngle, maxAngle);

            m_AimDirection = (Quaternion.Euler(0f, 0f, clampedAngle) * Vector2.up).normalized;

            if (m_CannonController != null)
            {
                m_CannonController.SetAimDirection(m_AimDirection);
            }

            Vector2 muzzlePos = m_CannonController != null && m_CannonController.MuzzlePoint != null
                ? (Vector2)m_CannonController.MuzzlePoint.position
                : pivot;

            // Reticle follows along clamped direction at target distance, pulled back along the
            // ray (never sideways) so it stays inside the arena and on the ball's path
            float distance = Mathf.Clamp((worldTarget - muzzlePos).magnitude, 2.0f, 12.0f);
            float maxDistance = DistanceInsideArena(muzzlePos, m_AimDirection);
            float minDistance = Mathf.Min((GameConstants.ReturnLineY + 0.5f - muzzlePos.y) / m_AimDirection.y, maxDistance);
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
            m_CurrentReticlePos = muzzlePos + m_AimDirection * distance;

            if (m_AimReticle != null)
            {
                m_AimReticle.position = new Vector3(m_CurrentReticlePos.x, m_CurrentReticlePos.y, 0f);
            }

            if (m_SightLine != null)
            {
                m_SightLine.Set(muzzlePos, m_CurrentReticlePos);
            }

            OnAimChanged?.Invoke(m_AimDirection);
        }

        /// <summary>Re-aims at the ✕ after the cannon slid along its rail — the ✕ stays put, the cannon turns.</summary>
        public void RefreshAim()
        {
            SetAimTarget(m_CurrentReticlePos);
        }

        private Vector2 GetAimPivot()
        {
            if (m_CannonController == null) return Vector2.zero;
            if (m_CannonController.CannonHead != null) return m_CannonController.CannonHead.position;
            return m_CannonController.MuzzlePoint.position;
        }

        private static float DistanceInsideArena(Vector2 from, Vector2 dir)
        {
            const float arenaHalfWidth = 4.8f;
            const float arenaTop = 13.0f;

            float t = dir.y > 1e-4f ? (arenaTop - from.y) / dir.y : float.MaxValue;
            if (dir.x > 1e-4f) t = Mathf.Min(t, (arenaHalfWidth - from.x) / dir.x);
            else if (dir.x < -1e-4f) t = Mathf.Min(t, (-arenaHalfWidth - from.x) / dir.x);
            return Mathf.Max(t, 0f);
        }

        public void SetReticleHolding(bool holding)
        {
            m_IsHolding = holding;
            if (m_AimReticle != null)
            {
                float targetScale = holding ? m_HoldingScale : m_NormalScale;
                m_AimReticle.localScale = new Vector3(targetScale, targetScale, 1.0f);
            }
        }

        public void SetAimActive(bool active)
        {
            m_IsActive = active;
            if (m_SightLine != null)
            {
                m_SightLine.SetVisible(active);
            }
            if (m_AimReticle != null)
            {
                m_AimReticle.gameObject.SetActive(active);
            }
        }
    }
}
