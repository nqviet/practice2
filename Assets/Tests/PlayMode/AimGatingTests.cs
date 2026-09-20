using System.Collections;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Cannon;
using Game.Runtime.GameFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class AimGatingTests
    {
        private GameObject m_RootGo;
        private GameManager m_GameManager;
        private BallStream m_BallStream;
        private AimController m_AimController;
        private CannonController m_CannonController;
        private InputReader m_InputReader;
        private BallPicker m_BallPicker;
        private GameplayConfig m_Config;
        private Ball m_BallPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_RootGo = new GameObject("TestAimGatingRoot");

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

            // BallPicker
            var pickerGo = new GameObject("BallPicker");
            pickerGo.transform.SetParent(m_RootGo.transform);
            m_BallPicker = pickerGo.AddComponent<BallPicker>();
            m_BallPicker.SetReferences(m_BallStream, m_CannonController);
            m_BallPicker.Build(new[] { GameColor.Red, GameColor.Blue, GameColor.Yellow });

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
        public IEnumerator DragNeverFires_PointerDownDragAndUp_NeverLaunchesBall()
        {
            bool ballFired = false;
            m_BallStream.OnBallFired = (b) => ballFired = true;

            // Simulate pointer drag on field (above return line)
            Vector2 aimStart = new Vector2(0f, 5f);
            Vector2 aimDrag = new Vector2(2f, 6f);

            m_InputReader.HandlePointerDown(aimStart);
            Assert.IsTrue(m_InputReader.Aiming, "InputReader should be in aiming mode");
            Assert.AreEqual(GameState.Aiming, m_GameManager.State, "State should transition to Aiming");

            m_InputReader.HandlePointerDrag(aimDrag);
            Assert.IsFalse(ballFired, "Dragging must not fire the ball");
            Assert.IsFalse(m_BallStream.IsBallInFlight, "Ball must not be in flight during drag");

            // Release drag
            m_InputReader.HandlePointerUp(aimDrag);

            yield return null;

            // CRITICAL INVARIANT TEST
            Assert.IsFalse(ballFired, "Releasing an aim drag must NEVER fire the ball (Decision C5)");
            Assert.IsFalse(m_BallStream.IsBallInFlight, "Ball must not be in flight after releasing drag");
            Assert.IsFalse(m_InputReader.Aiming, "Aiming flag should be cleared after pointer up");
        }

        [UnityTest]
        public IEnumerator TriggerFire_LaunchesBall_AndTransitionsStateToFiring()
        {
            bool ballFired = false;
            m_BallStream.OnBallFired = (b) => ballFired = true;

            m_InputReader.TriggerFire();

            yield return null;

            Assert.IsTrue(ballFired, "TriggerFire should launch the ball");
            Assert.IsTrue(m_BallStream.IsBallInFlight, "BallStream should report ball in flight");
            Assert.AreEqual(GameState.Firing, m_GameManager.State, "State should transition to Firing");
        }

        [UnityTest]
        public IEnumerator InputGating_WhileFiring_BlocksAimAndFire_AllowsFastForward()
        {
            // Enter Firing state
            m_InputReader.TriggerFire();
            yield return null;

            Assert.AreEqual(GameState.Firing, m_GameManager.State);
            Assert.IsFalse(m_InputReader.CanAim, "Aiming must be disabled while firing");
            Assert.IsFalse(m_InputReader.CanFire, "Firing must be disabled while already firing");
            Assert.IsTrue(m_InputReader.CanFastForward, "Fast forward must be enabled while firing");

            // Tapping during flight triggers fast forward
            bool fastForwardFired = false;
            m_InputReader.OnFastForward = () => fastForwardFired = true;

            m_InputReader.HandlePointerDown(new Vector2(0f, 5f));
            yield return null;

            Assert.IsTrue(fastForwardFired, "Tapping during Firing state should invoke FastForward");
            Assert.IsTrue(m_BallStream.CurrentBall.IsFastForward, "Ball should have fast forward active");
        }

        [UnityTest]
        public IEnumerator CannonRailDrag_ClampsWithinCannonRailHalfWidth()
        {
            // Drag cannon along the rail zone (< ReturnLineY)
            float railY = GameConstants.ReturnLineY - 0.5f;

            // Drag within bounds
            m_InputReader.HandlePointerDown(new Vector2(2.5f, railY));
            m_InputReader.HandlePointerDrag(new Vector2(3.0f, railY));
            Assert.AreEqual(3.0f, m_CannonController.RailX, 1e-3f);

            // Drag beyond right boundary (+4.4f)
            m_InputReader.HandlePointerDrag(new Vector2(10.0f, railY));
            Assert.AreEqual(m_Config.CannonRailHalfWidth, m_CannonController.RailX, 1e-3f, "RailX should be clamped to cannonRailHalfWidth");

            // Drag beyond left boundary (-4.4f)
            m_InputReader.HandlePointerDrag(new Vector2(-10.0f, railY));
            Assert.AreEqual(-m_Config.CannonRailHalfWidth, m_CannonController.RailX, 1e-3f, "RailX should be clamped to -cannonRailHalfWidth");

            m_InputReader.HandlePointerUp(new Vector2(-10.0f, railY));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BallPicker_LoadsSelectedColor_IntoBallStreamAndCannon()
        {
            Assert.AreEqual(GameColor.Red, m_BallPicker.Selected);
            Assert.AreEqual(GameColor.Red, m_BallStream.LoadedKind);

            m_BallPicker.SelectColor(GameColor.Blue);
            yield return null;

            Assert.AreEqual(GameColor.Blue, m_BallPicker.Selected);
            Assert.AreEqual(GameColor.Blue, m_BallStream.LoadedKind);

            m_BallPicker.SelectColor(GameColor.Yellow);
            yield return null;

            Assert.AreEqual(GameColor.Yellow, m_BallPicker.Selected);
            Assert.AreEqual(GameColor.Yellow, m_BallStream.LoadedKind);
        }
    }
}
