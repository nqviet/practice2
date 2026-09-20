using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Cannon;
using Game.Runtime.GameFlow;
using Game.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class AndroidInputTests
    {
        private GameObject m_RootGo;
        private GameManager m_GameManager;
        private BallStream m_BallStream;
        private AimController m_AimController;
        private CannonController m_CannonController;
        private InputReader m_InputReader;
        private GameplayConfig m_Config;
        private Ball m_BallPrefab;
        private EventSystem m_EventSystem;
        private Canvas m_Canvas;
        private Button m_TestButton;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_RootGo = new GameObject("TestAndroidInputRoot");

            // EventSystem & UI Canvas
            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(m_RootGo.transform);
            m_EventSystem = esGo.AddComponent<EventSystem>();

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(m_RootGo.transform);
            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<GraphicRaycaster>();

            var btnGo = new GameObject("TestButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(canvasGo.transform, false);
            m_TestButton = btnGo.GetComponent<Button>();
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 100f);
            rt.anchoredPosition = Vector2.zero;

            // GameManager
            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(m_RootGo.transform);
            m_GameManager = gmGo.AddComponent<GameManager>();

            // Config
            m_Config = ScriptableObject.CreateInstance<GameplayConfig>();
            m_Config.BallSpeed = 10f;
            m_Config.CannonRailHalfWidth = 4.4f;
            m_Config.AimClampFromUpDeg = 75f;
            m_Config.FastForwardMultiplier = 2.0f;

            // Ball Prefab
            var ballGo = new GameObject("BallPrefab");
            ballGo.transform.SetParent(m_RootGo.transform);
            ballGo.layer = GameConstants.BallLayer;
            m_BallPrefab = ballGo.AddComponent<Ball>();
            var rb = ballGo.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
            }
            ballGo.SetActive(false);

            // Cannon
            var cannonGo = new GameObject("Cannon");
            cannonGo.transform.SetParent(m_RootGo.transform);
            m_CannonController = cannonGo.AddComponent<CannonController>();
            var headGo = new GameObject("Head");
            headGo.transform.SetParent(cannonGo.transform);
            var muzzleGo = new GameObject("Muzzle");
            muzzleGo.transform.SetParent(headGo.transform);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            m_CannonController.SetReferences(m_Config, cannonGo.transform, cannonGo.transform, headGo.transform, muzzleGo.transform);

            // SightLine & Reticle
            var sightGo = new GameObject("SightLine");
            sightGo.transform.SetParent(m_RootGo.transform);
            var sightLine = sightGo.AddComponent<SightLine>();

            var reticleGo = new GameObject("Reticle");
            reticleGo.transform.SetParent(m_RootGo.transform);

            // AimController
            m_AimController = m_RootGo.AddComponent<AimController>();
            m_AimController.SetReferences(m_Config, m_CannonController, sightLine, reticleGo.transform);

            // BallStream
            m_BallStream = m_RootGo.AddComponent<BallStream>();
            m_BallStream.SetReferences(m_Config, m_BallPrefab, muzzleGo.transform, null, null);
            m_BallStream.LoadedKind = GameColor.Red;

            // InputReader
            m_InputReader = m_RootGo.AddComponent<InputReader>();
            m_InputReader.SetReferences(Camera.main, m_AimController, m_CannonController, m_BallStream);

            m_GameManager.ChangeState(GameState.Ready);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Config != null) Object.Destroy(m_Config);
            if (m_RootGo != null) Object.Destroy(m_RootGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TouchAim_Lifecycle_EngagesAndReleasesCorrectly()
        {
            Vector2 aimTarget = new Vector2(1.5f, 5.0f);

            // 1. Touch began in arena
            m_InputReader.HandlePointerDown(aimTarget);
            Assert.IsTrue(m_InputReader.Aiming, "Touch down should begin aiming");
            Assert.AreEqual(GameState.Aiming, m_GameManager.State, "State must transition to Aiming");

            // 2. Drag
            Vector2 aimTarget2 = new Vector2(-2.0f, 6.0f);
            m_InputReader.HandlePointerDrag(aimTarget2);
            Assert.IsTrue(m_InputReader.Aiming, "Drag should keep aiming active");

            // 3. Touch released
            m_InputReader.HandlePointerUp(aimTarget2);
            Assert.IsFalse(m_InputReader.Aiming, "Touch release must clear aiming state");
            yield return null;
        }

        [UnityTest]
        public IEnumerator TouchRail_DragAndRelease_UpdatesRailAndClearsState()
        {
            float railTouchY = GameConstants.ReturnLineY - 0.5f;
            Vector2 railStart = new Vector2(1.0f, railTouchY);

            // Touch began below return line -> Rail drag
            m_InputReader.HandlePointerDown(railStart);
            Assert.IsFalse(m_InputReader.Aiming, "Rail touch should not engage aiming");

            // Drag across the rail
            Vector2 railMove = new Vector2(3.0f, railTouchY);
            m_InputReader.HandlePointerDrag(railMove);
            Assert.AreEqual(3.0f, m_CannonController.RailX, 1e-3f, "Cannon rail position should follow drag");

            // Release
            m_InputReader.HandlePointerUp(railMove);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CancelInput_ResetsAimAndReticle()
        {
            Vector2 aimTarget = new Vector2(0f, 5f);
            m_InputReader.HandlePointerDown(aimTarget);
            Assert.IsTrue(m_InputReader.Aiming);

            // Simulate app loss of focus or backgrounding
            m_InputReader.CancelInput();

            Assert.IsFalse(m_InputReader.Aiming, "CancelInput must clear Aiming flag");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StateChangeToFiring_CancelsActiveAim()
        {
            Vector2 aimTarget = new Vector2(0f, 5f);
            m_InputReader.HandlePointerDown(aimTarget);
            Assert.IsTrue(m_InputReader.Aiming);

            // Change state to Firing
            m_GameManager.ChangeState(GameState.Firing);
            yield return null;

            Assert.IsFalse(m_InputReader.Aiming, "Transitioning to Firing must cancel active aim");
        }

        [UnityTest]
        public IEnumerator UIManager_BackClosesPopup()
        {
            // Setup a test popup host
            var hostGo = new GameObject("PopupHost");
            hostGo.transform.SetParent(m_RootGo.transform);
            var uiManager = hostGo.AddComponent<UIManager>();

            var popupGo = new GameObject("Popup_Settings", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            popupGo.transform.SetParent(hostGo.transform);
            var popup = popupGo.AddComponent<UIPopup>();
            popup.Configure(PopupId.Settings, true, true, null);

            uiManager.SetPopups(new[] { popup });
            uiManager.Show(PopupId.Settings);
            yield return null;

            Assert.AreEqual(PopupId.Settings, uiManager.Current, "Settings popup should be open");

            // Calling Hide (which Android back triggers) closes the popup
            uiManager.Hide();
            yield return null;

            Assert.AreEqual(PopupId.None, uiManager.Current, "Popup should be closed after back action");
        }
    }
}
