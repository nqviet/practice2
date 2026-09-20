using Game.Board;
using Game.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Runtime.Ball
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Ball : MonoBehaviour
    {
        private static readonly int s_SolidMask = (1 << GameConstants.BlockLayer) | (1 << GameConstants.WallLayer);

        [SerializeField] private float m_BaseSpeed = 14.0f;
        [SerializeField] private float m_ReturnLineY = GameConstants.ReturnLineY;
        [SerializeField] private float m_ReturnMargin = 0.35f;
        [SerializeField] private GameColor m_Color = GameColor.White;

        private Rigidbody2D m_Rigidbody;
        private CircleCollider2D m_Collider;
        private SpriteRenderer m_SpriteRenderer;
        private BallTrail m_Trail;

        private float m_TargetSpeed;
        private bool m_IsInFlight;
        private bool m_IsFastForward;
        private Vector2 m_LastVelocity;

        public float Speed => m_TargetSpeed;
        public GameColor Color => m_Color;
        public bool IsInFlight => m_IsInFlight;
        public bool IsFastForward => m_IsFastForward;
        public BallTrail Trail => m_Trail;

        public UnityAction<Block> OnBlockHit;
        public UnityAction OnReturned;
        public UnityAction OnBounced;

        private void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody2D>();
            m_Collider = GetComponent<CircleCollider2D>();
            m_SpriteRenderer = GetComponent<SpriteRenderer>();
            m_Trail = GetComponent<BallTrail>();

            ConfigurePhysicsBody();
        }

        private void ConfigurePhysicsBody()
        {
            if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponent<Rigidbody2D>();
            }

            if (m_Collider == null)
            {
                m_Collider = GetComponent<CircleCollider2D>();
            }

            if (m_Rigidbody != null)
            {
                m_Rigidbody.bodyType = RigidbodyType2D.Dynamic;
                m_Rigidbody.gravityScale = 0f;
                m_Rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
                m_Rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
                m_Rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                m_Rigidbody.linearDamping = 0f;
            }

            if (m_Collider != null)
            {
                m_Collider.radius = 0.28f;
            }

            gameObject.layer = GameConstants.BallLayer;
        }

        public void Configure(float baseSpeed, float returnLineY, float returnMargin)
        {
            m_BaseSpeed = baseSpeed > 0 ? baseSpeed : 14.0f;
            m_ReturnLineY = returnLineY;
            m_ReturnMargin = returnMargin;
        }

        public void Launch(Vector2 direction, float speed, GameColor color, BallSkin skin = null)
        {
            m_Color = color;
            m_BaseSpeed = speed > 0 ? speed : 14.0f;
            m_TargetSpeed = m_BaseSpeed;
            m_IsFastForward = false;
            m_IsInFlight = true;

            ApplySkin(skin);

            if (direction.sqrMagnitude < 1e-4f)
            {
                direction = Vector2.up;
            }

            if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponent<Rigidbody2D>();
            }

            if (m_Rigidbody != null)
            {
                m_Rigidbody.linearVelocity = direction.normalized * m_TargetSpeed;
                m_LastVelocity = m_Rigidbody.linearVelocity;
            }

            if (m_Trail != null)
            {
                m_Trail.Begin(transform.position, m_Color, skin);
            }
        }

        public void SetFastForward(bool enabled, float multiplier = 2.0f)
        {
            m_IsFastForward = enabled;
            m_TargetSpeed = m_BaseSpeed * (enabled ? multiplier : 1.0f);

            if (m_IsInFlight && m_Rigidbody != null && m_Rigidbody.linearVelocity.sqrMagnitude > 1e-4f)
            {
                m_Rigidbody.linearVelocity = m_Rigidbody.linearVelocity.normalized * m_TargetSpeed;
                m_LastVelocity = m_Rigidbody.linearVelocity;
            }
        }

        public void Stop()
        {
            m_IsInFlight = false;
            if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponent<Rigidbody2D>();
            }

            if (m_Rigidbody != null)
            {
                m_Rigidbody.linearVelocity = Vector2.zero;
            }
            m_LastVelocity = Vector2.zero;

            if (m_Trail != null)
            {
                m_Trail.EndShot();
            }
        }

        private void FixedUpdate()
        {
            if (!m_IsInFlight) return;

            // 1. Authoritative Constant-Speed Invariant (§4.2)
            if (m_Rigidbody != null)
            {
                Vector2 vel = m_Rigidbody.linearVelocity;
                float currentSpeed = vel.magnitude;
                if (currentSpeed > 0.001f)
                {
                    if (Mathf.Abs(currentSpeed - m_TargetSpeed) > 1e-3f)
                    {
                        m_Rigidbody.linearVelocity = vel.normalized * m_TargetSpeed;
                    }
                }
                else if (m_LastVelocity.sqrMagnitude > 0.001f)
                {
                    m_Rigidbody.linearVelocity = m_LastVelocity.normalized * m_TargetSpeed;
                }

                m_LastVelocity = m_Rigidbody.linearVelocity;
            }

            // 2. Authoritative Geometric Return Detection (§4.2)
            // The muzzle sits below the return line, so only a descending ball can end the shot
            if (IsDescending && transform.position.y < m_ReturnLineY - m_ReturnMargin)
            {
                HandleReturn();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!m_IsInFlight) return;

            // Multi-contact averaging for corners (§4.2)
            Vector2 normal = Vector2.zero;
            int count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                normal += collision.GetContact(i).normal;
            }

            if (normal.sqrMagnitude > 1e-6f)
            {
                normal.Normalize();
            }
            else if (count > 0)
            {
                normal = collision.GetContact(0).normal;
            }
            else
            {
                normal = -m_LastVelocity.normalized;
            }

            normal = ResolveSeamNormal(collision.collider, normal);

            // Reflect and re-normalize to targetSpeed
            Vector2 inVel = m_LastVelocity;
            if (inVel.sqrMagnitude < 1e-4f)
            {
                inVel = collision.relativeVelocity;
            }
            if (inVel.sqrMagnitude < 1e-4f && m_Rigidbody != null)
            {
                inVel = m_Rigidbody.linearVelocity;
            }

            // Already moving away from this surface: a second collider of the same flat wall (or its
            // seam) reported in the same step as the one we just bounced off. Reflecting again would
            // flip the ball back into the wall, so keep the outgoing velocity and only report the hit.
            if (Vector2.Dot(inVel, normal) >= 0f)
            {
                if (m_Rigidbody != null && inVel.sqrMagnitude > 1e-4f)
                {
                    m_Rigidbody.linearVelocity = inVel.normalized * m_TargetSpeed;
                }
            }
            else
            {
                Vector2 reflected = Vector2.Reflect(inVel, normal);
                if (reflected.sqrMagnitude < 1e-4f)
                {
                    reflected = normal;
                }
                reflected = reflected.normalized * m_TargetSpeed;

                if (m_Rigidbody != null)
                {
                    m_Rigidbody.linearVelocity = reflected;
                }
                m_LastVelocity = reflected;

                // Notify trail
                Vector2 contactPoint = count > 0 ? collision.GetContact(0).point : (Vector2)transform.position;
                if (m_Trail != null)
                {
                    m_Trail.OnBounce(contactPoint);
                }

                OnBounced?.Invoke();
                ServiceLocator.Get<IAudioService>()?.Play(SfxId.Bounce, 1.0f + (m_TargetSpeed - 14.0f) * 0.02f);
                ServiceLocator.Get<IVFXService>()?.PlayImpact(contactPoint);
            }

            // Contact reporting with Block
            Block block = collision.collider.GetComponent<Block>();
            if (block == null)
            {
                block = collision.gameObject.GetComponent<Block>();
            }

            if (block != null)
            {
                OnBlockHit?.Invoke(block);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!m_IsInFlight) return;

            Vector2 normal = Vector2.zero;
            int count = collision.contactCount;
            for (int i = 0; i < count; i++)
            {
                normal += collision.GetContact(i).normal;
            }

            if (normal.sqrMagnitude > 1e-6f)
            {
                normal = ResolveSeamNormal(collision.collider, normal.normalized);
                if (m_Rigidbody != null && Vector2.Dot(m_Rigidbody.linearVelocity, normal) < 0f)
                {
                    Vector2 reflected = Vector2.Reflect(m_Rigidbody.linearVelocity, normal);
                    if (reflected.sqrMagnitude < 1e-4f)
                    {
                        reflected = normal;
                    }
                    m_Rigidbody.linearVelocity = reflected.normalized * m_TargetSpeed;
                    m_LastVelocity = m_Rigidbody.linearVelocity;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!m_IsInFlight) return;

            // Secondary check: Sensor / ReturnLine trigger
            if (other.gameObject.layer == GameConstants.SensorLayer)
            {
                if (IsDescending && transform.position.y < m_ReturnLineY)
                {
                    HandleReturn();
                }
            }
        }

        private bool IsDescending => m_Rigidbody != null && m_Rigidbody.linearVelocity.y < 0f;

        /// <summary>
        /// Blocks are flush full-cell boxes, so where two of them form a flat face the physics engine
        /// can report the corner of one of them (a "ghost" contact at the seam) and the ball skews off
        /// a straight wall. A corner whose neighbour along one axis is solid is not really exposed:
        /// bounce off the face instead. Truly exposed corners keep their diagonal normal.
        /// </summary>
        private static Vector2 ResolveSeamNormal(Collider2D collider, Vector2 normal)
        {
            if (collider == null || Mathf.Abs(normal.x) < 0.01f || Mathf.Abs(normal.y) < 0.01f) return normal;
            if (collider.GetComponent<Block>() == null) return normal;

            Vector2 center = collider.bounds.center;
            float cell = GameConstants.CellSize;
            bool solidX = Physics2D.OverlapPoint(center + new Vector2(Mathf.Sign(normal.x) * cell, 0f), s_SolidMask) != null;
            bool solidY = Physics2D.OverlapPoint(center + new Vector2(0f, Mathf.Sign(normal.y) * cell), s_SolidMask) != null;

            if (solidY && !solidX) return new Vector2(Mathf.Sign(normal.x), 0f);
            if (solidX && !solidY) return new Vector2(0f, Mathf.Sign(normal.y));
            return normal;
        }

        private void HandleReturn()
        {
            Stop();
            OnReturned?.Invoke();
        }

        public void ApplySkin(BallSkin skin)
        {
            if (m_SpriteRenderer == null)
            {
                m_SpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (skin != null && skin.Sprite != null && m_SpriteRenderer != null)
            {
                m_SpriteRenderer.sprite = skin.Sprite;
            }
        }
    }
}
