using System.Collections.Generic;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.GameFlow;
using Game.Runtime.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Game.Runtime.Cannon
{
    public class InputReader : MonoBehaviour
    {
        private static readonly List<RaycastResult> s_RaycastResults = new List<RaycastResult>();

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
        private int m_ActiveTouchId = -1;
        private bool m_IsMouseDragging;
        private Vector2 m_LastWorldPos;

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

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }

            CancelInput();
            EnhancedTouchSupport.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelInput();
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state != GameState.Ready && state != GameState.Aiming)
            {
                CancelInput();
            }
        }

        private void Update()
        {
            HandleHardwareInput();
        }

        private void HandleHardwareInput()
        {
            // 1. Support Touch via EnhancedTouch (Android, iOS, touchscreen devices)
            bool touchActive = false;
            if (Touch.activeTouches.Count > 0)
            {
                touchActive = true;

                if (m_ActiveTouchId == -1)
                {
                    // Look for a touch that began this frame
                    for (int i = 0; i < Touch.activeTouches.Count; i++)
                    {
                        var touch = Touch.activeTouches[i];
                        if (touch.phase == TouchPhase.Began)
                        {
                            // Filter touches over interactive UI
                            if (IsPointerOverUI(touch.screenPosition, touch.touchId))
                            {
                                continue;
                            }

                            m_ActiveTouchId = touch.touchId;
                            Vector2 worldPos = ScreenToWorld(touch.screenPosition);
                            m_LastWorldPos = worldPos;
                            HandlePointerDown(worldPos);
                            break;
                        }
                    }
                }
                else
                {
                    // Follow the active touch
                    bool touchFound = false;
                    for (int i = 0; i < Touch.activeTouches.Count; i++)
                    {
                        var touch = Touch.activeTouches[i];
                        if (touch.touchId == m_ActiveTouchId)
                        {
                            touchFound = true;
                            Vector2 worldPos = ScreenToWorld(touch.screenPosition);
                            m_LastWorldPos = worldPos;

                            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                            {
                                if (m_IsAiming || m_IsDraggingRail)
                                {
                                    HandlePointerDrag(worldPos);
                                }
                            }
                            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                            {
                                if (m_IsAiming || m_IsDraggingRail)
                                {
                                    HandlePointerUp(worldPos);
                                }
                                else
                                {
                                    CancelInput();
                                }
                            }
                            break;
                        }
                    }

                    if (!touchFound)
                    {
                        // Active touch was terminated or lost by the OS
                        if (m_IsAiming || m_IsDraggingRail)
                        {
                            HandlePointerUp(m_LastWorldPos);
                        }
                        else
                        {
                            CancelInput();
                        }
                    }
                }
            }
            else if (m_ActiveTouchId != -1)
            {
                // All touches ended
                if (m_IsAiming || m_IsDraggingRail)
                {
                    HandlePointerUp(m_LastWorldPos);
                }
                else
                {
                    CancelInput();
                }
            }

            // 2. Fallback / Desktop: Mouse input (Unity Editor, PC standalone)
            if (!touchActive && Mouse.current != null)
            {
                Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
                bool isDown = Mouse.current.leftButton.wasPressedThisFrame;
                bool isHeld = Mouse.current.leftButton.isPressed;
                bool isUp = Mouse.current.leftButton.wasReleasedThisFrame;

                if (isDown)
                {
                    if (!IsPointerOverUI(mouseScreenPos, -1))
                    {
                        m_IsMouseDragging = true;
                        Vector2 worldPos = ScreenToWorld(mouseScreenPos);
                        m_LastWorldPos = worldPos;
                        HandlePointerDown(worldPos);
                    }
                }
                else if (m_IsMouseDragging)
                {
                    Vector2 worldPos = ScreenToWorld(mouseScreenPos);
                    m_LastWorldPos = worldPos;

                    if (isHeld && (m_IsAiming || m_IsDraggingRail))
                    {
                        HandlePointerDrag(worldPos);
                    }
                    else if (isUp)
                    {
                        if (m_IsAiming || m_IsDraggingRail)
                        {
                            HandlePointerUp(worldPos);
                        }
                        m_IsMouseDragging = false;
                    }
                }
                else if (isUp && (m_IsAiming || m_IsDraggingRail))
                {
                    Vector2 worldPos = ScreenToWorld(mouseScreenPos);
                    HandlePointerUp(worldPos);
                    m_IsMouseDragging = false;
                }
            }

            // 3. Hardware Keyboard Shortcuts (Accessibility & Editor Testing)
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    if (CanFire)
                    {
                        TriggerFire();
                    }
                    else if (CanFastForward)
                    {
                        TriggerFastForward();
                    }
                }

                if (Keyboard.current.rKey.wasPressedThisFrame)
                {
                    TriggerRestart();
                }
            }
        }

        private bool IsPointerOverUI(Vector2 screenPosition, int touchOrPointerId = -1)
        {
            if (EventSystem.current == null) return false;

            // Direct raycast against UI canvas hierarchy at screenPosition
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            s_RaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, s_RaycastResults);

            for (int i = 0; i < s_RaycastResults.Count; i++)
            {
                var go = s_RaycastResults[i].gameObject;
                if (go != null && go.GetComponent<ScreenFeedback>() == null)
                {
                    return true;
                }
            }

            // Direct EventSystem pointer check
            if (touchOrPointerId >= 0 && EventSystem.current.IsPointerOverGameObject(touchOrPointerId))
            {
                return true;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            return false;
        }

        private Vector2 ScreenToWorld(Vector2 screenPos)
        {
            if (m_Camera == null) m_Camera = Camera.main;
            if (m_Camera == null) m_Camera = FindFirstObjectByType<Camera>();
            if (m_Camera == null) return m_LastWorldPos;

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

            m_LastWorldPos = worldPos;
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

            m_LastWorldPos = worldPos;
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
            m_LastWorldPos = worldPos;
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
            m_ActiveTouchId = -1;
            m_IsMouseDragging = false;
        }

        public void CancelInput()
        {
            if (m_IsAiming)
            {
                m_IsAiming = false;
                if (m_AimController != null)
                {
                    m_AimController.SetReticleHolding(false);
                }
            }

            m_IsDraggingRail = false;
            m_ActiveTouchId = -1;
            m_IsMouseDragging = false;
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
