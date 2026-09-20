using System.Collections;
using Game.Core;
using UnityEngine;

namespace Game.Runtime.Cannon
{
    public class CannonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameplayConfig m_Config;
        [SerializeField] private Transform m_CannonRoot;
        [SerializeField] private Transform m_CannonBase;
        [SerializeField] private Transform m_CannonHead;
        [SerializeField] private Transform m_MuzzlePoint;
        [SerializeField] private SpriteRenderer m_LoadedBallRenderer;
        [SerializeField] private ParticleSystem m_MuzzleFlash;
        [SerializeField] private GameObject m_ArrowLeft;
        [SerializeField] private GameObject m_ArrowRight;

        private float m_RailX;
        private Vector2 m_CurrentAimDir = Vector2.up;
        private Coroutine m_RecoilCoroutine;
        private Vector3 m_RootOriginalLocalPos;
        private Quaternion m_RootOriginalLocalRot;

        public float RailX => transform.position.x;
        public Transform MuzzlePoint => m_MuzzlePoint != null ? m_MuzzlePoint : transform;
        public Transform CannonHead => m_CannonHead;
        public Transform CannonRoot => m_CannonRoot;
        public Vector2 CurrentAimDirection => m_CurrentAimDir;

        public void SetReferences(
            GameplayConfig config,
            Transform cannonRoot,
            Transform cannonBase,
            Transform cannonHead,
            Transform muzzlePoint,
            SpriteRenderer loadedBallRenderer = null,
            ParticleSystem muzzleFlash = null)
        {
            m_Config = config;
            m_CannonRoot = cannonRoot;
            m_CannonBase = cannonBase;
            m_CannonHead = cannonHead;
            m_MuzzlePoint = muzzlePoint;
            m_LoadedBallRenderer = loadedBallRenderer;
            m_MuzzleFlash = muzzleFlash;

            if (m_CannonRoot != null)
            {
                m_RootOriginalLocalPos = m_CannonRoot.localPosition;
                m_RootOriginalLocalRot = m_CannonRoot.localRotation;
            }
        }

        private void Awake()
        {
            if (m_CannonRoot != null)
            {
                m_RootOriginalLocalPos = m_CannonRoot.localPosition;
                m_RootOriginalLocalRot = m_CannonRoot.localRotation;
            }
            else
            {
                m_RootOriginalLocalPos = Vector3.zero;
                m_RootOriginalLocalRot = Quaternion.identity;
            }

            m_RailX = transform.position.x;

            if (m_ArrowLeft == null) m_ArrowLeft = FindChild("Arrow_Left");
            if (m_ArrowRight == null) m_ArrowRight = FindChild("Arrow_Right");
            UpdateArrowHints();
        }

        private GameObject FindChild(string childName)
        {
            Transform root = m_CannonRoot != null ? m_CannonRoot : transform;
            Transform child = root.Find(childName);
            return child != null ? child.gameObject : null;
        }

        /// <summary>At a rail end the outward arrow has nowhere to point (and would hang off-screen): hide it.</summary>
        private void UpdateArrowHints()
        {
            float halfWidth = m_Config != null ? m_Config.CannonRailHalfWidth : 4.4f;
            const float edgeEpsilon = 0.05f;

            if (m_ArrowLeft != null) m_ArrowLeft.SetActive(m_RailX > -halfWidth + edgeEpsilon);
            if (m_ArrowRight != null) m_ArrowRight.SetActive(m_RailX < halfWidth - edgeEpsilon);
        }

        public void SetRailX(float x)
        {
            float halfWidth = m_Config != null ? m_Config.CannonRailHalfWidth : 4.4f;
            m_RailX = Mathf.Clamp(x, -halfWidth, halfWidth);

            Vector3 pos = transform.position;
            pos.x = m_RailX;
            transform.position = pos;

            UpdateArrowHints();
        }

        public void SetAimDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 1e-4f) return;
            m_CurrentAimDir = direction.normalized;

            if (m_CannonHead != null)
            {
                float angle = Mathf.Atan2(m_CurrentAimDir.y, m_CurrentAimDir.x) * Mathf.Rad2Deg - 90f;
                m_CannonHead.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        public void SetLoadedKind(GameColor color, Sprite ballSprite = null)
        {
            if (m_LoadedBallRenderer != null)
            {
                m_LoadedBallRenderer.gameObject.SetActive(color != GameColor.None);
                if (ballSprite != null)
                {
                    // Skin sprites are already coloured: tinting them again would mix the colours
                    m_LoadedBallRenderer.sprite = ballSprite;
                    m_LoadedBallRenderer.color = Color.white;
                    return;
                }

                switch (color)
                {
                    case GameColor.Red:
                        m_LoadedBallRenderer.color = new Color(1f, 0.25f, 0.27f, 1f);
                        break;
                    case GameColor.Blue:
                        m_LoadedBallRenderer.color = new Color(0.2f, 0.55f, 1f, 1f);
                        break;
                    case GameColor.Yellow:
                        m_LoadedBallRenderer.color = new Color(1f, 0.85f, 0.15f, 1f);
                        break;
                    case GameColor.White:
                    default:
                        m_LoadedBallRenderer.color = Color.white;
                        break;
                }
            }
        }

        public void SetLoadedBallVisible(bool visible)
        {
            if (m_LoadedBallRenderer != null)
            {
                m_LoadedBallRenderer.enabled = visible;
            }
        }

        public void PlayRecoil()
        {
            if (m_MuzzleFlash != null)
            {
                m_MuzzleFlash.Play();
            }

            if (m_CannonRoot != null)
            {
                if (m_RecoilCoroutine != null)
                {
                    // Interrupted mid-kick: keep the rest pose captured by the first shot
                    StopCoroutine(m_RecoilCoroutine);
                }
                else
                {
                    // Rest pose is wherever CameraFitter / the rail put the cannon, not the Awake pose
                    m_RootOriginalLocalPos = m_CannonRoot.localPosition;
                    m_RootOriginalLocalRot = m_CannonRoot.localRotation;
                }
                m_RecoilCoroutine = StartCoroutine(RecoilRoutine());
            }
        }

        private IEnumerator RecoilRoutine()
        {
            // Spec §6.4: translate −0.15 u + rotate 3°, 0.12 s out / 0.18 s back
            const float kickDuration = 0.12f;
            const float returnDuration = 0.18f;
            const float kickDistance = -0.15f;
            const float kickRotation = 3.0f;

            Quaternion kickRot = m_RootOriginalLocalRot * Quaternion.Euler(0f, 0f, kickRotation);

            float t = 0f;
            while (t < kickDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / kickDuration);
                ApplyRecoilPose(kickDistance * progress, Quaternion.Slerp(m_RootOriginalLocalRot, kickRot, progress));
                yield return null;
            }

            t = 0f;
            while (t < returnDuration)
            {
                t += Time.deltaTime;
                float progress = Mathf.Clamp01(t / returnDuration);
                ApplyRecoilPose(kickDistance * (1f - progress), Quaternion.Slerp(kickRot, m_RootOriginalLocalRot, progress));
                yield return null;
            }

            ApplyRecoilPose(0f, m_RootOriginalLocalRot);
            m_RecoilCoroutine = null;
        }

        /// <summary>
        /// The recoil only kicks Y and rotation: the rail X is live (the shot may resolve and the
        /// player drag the cannon before the kick settles), so it is never restored from the rest pose.
        /// </summary>
        private void ApplyRecoilPose(float yOffset, Quaternion rotation)
        {
            Vector3 pos = m_CannonRoot.localPosition;
            pos.y = m_RootOriginalLocalPos.y + yOffset;
            pos.z = m_RootOriginalLocalPos.z;
            m_CannonRoot.localPosition = pos;
            m_CannonRoot.localRotation = rotation;
        }
    }
}
