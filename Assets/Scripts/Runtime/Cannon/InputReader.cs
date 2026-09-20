using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.GameFlow;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Runtime.Cannon
{
    public class InputReader : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera m_Camera;
        [SerializeField] private AimController m_AimController;
        [SerializeField] private CannonController m_CannonController;
        [SerializeField] private BallStream m_BallStream;

        public UnityAction<Vector2> OnPress;
        public UnityAction<Vector2> OnDrag;
        public UnityAction<Vector2> OnRelease;
        public UnityAction OnFire;
        public UnityAction OnRestart;
        public UnityAction OnFastForward;

        private bool m_IsAiming;
        private bool m_IsDraggingRail;

        public bool Aiming => m_IsAiming;

        public bool CanAim
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null) return true;
                if (gm.IsPaused) return false;
                return gm.State == GameState.Ready || gm.State == GameState.Aiming;
            }
        }

        public bool CanFire
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null) return true;
                if (gm.IsPaused) return false;
                return gm.State == GameState.Ready || gm.State == GameState.Aiming;
            }
        }

        public bool CanFastForward
        {
            get
            {
                var gm = GameManager.Instance;
                if (gm == null || gm.IsPaused) return false;
                return gm.State == GameState.Firing;
            }
        }

        public void SetReferences(
            Camera cam,
            AimController aimController,
            CannonController cannonController,
            BallStream ballStream)
        {
            m_Camera = cam;
            m_AimController = aimController;
            m_CannonController = cannonController;
            m_BallStream = ballStream;
        }

        private void Awake()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }
            if (m_AimController == null)
            {
                m_AimController = FindFirstObjectByType<AimController>();
            }
            if (m_CannonController == null)
            {
                m_CannonController = FindFirstObjectByType<CannonController>();
            }
            if (m_BallStream == null)
            {
                m_BallStream = FindFirstObjectByType<BallStream>();
            }
        }

        private void Update()
        {
            HandleHardwareInput();
        }

        private void HandleHardwareInput()
        {
            // Support Touch and Mouse via new Input System
            Vector2 screenPos = Vector2.zero;
            bool isDown = false;
            bool isHeld = false;
            bool isUp = false;

            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                isDown = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                isHeld = true;
                isUp = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            else if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                isDown = Mouse.current.leftButton.wasPressedThisFrame;
                isHeld = Mouse.current.leftButton.isPressed;
                isUp = Mouse.current.leftButton.wasReleasedThisFrame;
            }

            // Check if pointer is over a UI button (e.g. Fire, Picker, Settings)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // UI clicks are handled by uGUI
                return;
            }

            Vector2 worldPos = ScreenToWorld(screenPos);

            if (isDown)
            {
                HandlePointerDown(worldPos);
            }
            else if (isHeld && (m_IsAiming || m_IsDraggingRail))
            {
                HandlePointerDrag(worldPos);
            }
            else if (isUp && (m_IsAiming || m_IsDraggingRail))
            {
                HandlePointerUp(worldPos);
            }
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) return screenPos;

            Vector3 world = m_Camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -m_Camera.transform.position.z));
            return new Vector2(world.x, world.y);
        }

        public void HandlePointerDown(Vector2 worldPos)
        {
            if (CanFastForward)
            {
                TriggerFastForward();
                return;
            }

            if (!CanAim) return;

            OnPress?.Invoke(worldPos);

            // Determine if touching the cannon / bottom rail or the field
            float railZoneY = GameConstants.ReturnLineY;
            if (worldPos.y < railZoneY)
            {
                m_IsDraggingRail = true;
                MoveRail(worldPos.x);
            }
            else
            {
                m_IsAiming = true;
                if (GameManager.Instance != null && GameManager.Instance.State == GameState.Ready)
                {
                    GameManager.Instance.ChangeState(GameState.Aiming);
                }

                if (m_AimController != null)
                {
                    m_AimController.SetReticleHolding(true);
                    m_AimController.SetAimTarget(worldPos);
                }
            }
        }

        public void HandlePointerDrag(Vector2 worldPos)
        {
            if (!CanAim) return;

            OnDrag?.Invoke(worldPos);

            if (m_IsDraggingRail)
            {
                MoveRail(worldPos.x);
            }
            else if (m_IsAiming)
            {
                if (m_AimController != null)
                {
                    m_AimController.SetAimTarget(worldPos);
                }
            }
        }

        private void MoveRail(float x)
        {
            if (m_CannonController != null)
            {
                m_CannonController.SetRailX(x);
            }

            // The ✕ is the aim: keep it where it is and turn the cannon toward it from the new spot
            if (m_AimController != null)
            {
                m_AimController.RefreshAim();
            }
        }

        public void HandlePointerUp(Vector2 worldPos)
        {
            // CRITICAL INVARIANT: Releasing an aim drag NEVER fires. (Decision C5)
            OnRelease?.Invoke(worldPos);

            if (m_IsAiming)
            {
                m_IsAiming = false;
                if (m_AimController != null)
                {
                    m_AimController.SetReticleHolding(false);
                }
            }

            m_IsDraggingRail = false;
        }

        public void TriggerFire()
        {
            if (!CanFire) return;

            if (m_BallStream != null && m_AimController != null)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Firing);
                }

                if (m_CannonController != null)
                {
                    m_CannonController.PlayRecoil();
                }

                m_BallStream.Fire(m_AimController.AimDirection);
            }

            OnFire?.Invoke();
        }

        public void TriggerRestart()
        {
            OnRestart?.Invoke();
        }

        public void TriggerFastForward()
        {
            if (!CanFastForward) return;

            if (m_BallStream != null)
            {
                m_BallStream.SetFastForward(true);
            }

            OnFastForward?.Invoke();
        }
    }
}
