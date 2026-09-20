using System.Collections;
using Game.Core;
using Game.Runtime.Cannon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class AimControllerTests
    {
        private GameObject m_RootGo;
        private AimController m_AimController;
        private CannonController m_CannonController;
        private SightLine m_SightLine;
        private Transform m_ReticleTransform;
        private GameplayConfig m_Config;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_RootGo = new GameObject("TestAimRoot");

            m_Config = ScriptableObject.CreateInstance<GameplayConfig>();
            m_Config.AimClampFromUpDeg = 75.0f;

            // Cannon
            var cannonGo = new GameObject("TestCannon");
            cannonGo.transform.SetParent(m_RootGo.transform);
            m_CannonController = cannonGo.AddComponent<CannonController>();

            var headGo = new GameObject("Head");
            headGo.transform.SetParent(cannonGo.transform);
            var muzzleGo = new GameObject("Muzzle");
            muzzleGo.transform.SetParent(headGo.transform);
            muzzleGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);

            m_CannonController.SetReferences(m_Config, cannonGo.transform, cannonGo.transform, headGo.transform, muzzleGo.transform);

            // SightLine
            var sightGo = new GameObject("SightLine");
            sightGo.transform.SetParent(m_RootGo.transform);
            m_SightLine = sightGo.AddComponent<SightLine>();

            // Reticle
            var reticleGo = new GameObject("Reticle");
            reticleGo.transform.SetParent(m_RootGo.transform);
            m_ReticleTransform = reticleGo.transform;

            // AimController
            m_AimController = m_RootGo.AddComponent<AimController>();
            m_AimController.SetReferences(m_Config, m_CannonController, m_SightLine, m_ReticleTransform);

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
        public IEnumerator AimController_StraightUp_AimsUp()
        {
            m_AimController.SetAimTarget(new Vector2(0f, 5f));
            yield return null;

            Assert.AreEqual(0f, m_AimController.AimDirection.x, 1e-3f);
            Assert.AreEqual(1f, m_AimController.AimDirection.y, 1e-3f);
        }

        [UnityTest]
        public IEnumerator AimController_ExtremeAngle_ClampsToMaxDegFromUp()
        {
            // Point completely sideways to the right (x=10, y=0 from muzzle)
            Vector2 muzzlePos = m_CannonController.MuzzlePoint.position;
            m_AimController.SetAimTarget(muzzlePos + new Vector2(10f, 0f));
            yield return null;

            float angleFromUp = Vector2.Angle(Vector2.up, m_AimController.AimDirection);
            Assert.LessOrEqual(angleFromUp, m_Config.AimClampFromUpDeg + 1e-3f,
                $"Aim angle ({angleFromUp}) must not exceed clamp ({m_Config.AimClampFromUpDeg})");
            Assert.Greater(m_AimController.AimDirection.y, 0f, "Y must always be positive (never downward or sideways)");
        }

        [UnityTest]
        public IEnumerator AimController_DownwardTarget_ClampsToUpwardArc()
        {
            // Point downwards (e.g. into the ground / cannon base)
            Vector2 muzzlePos = m_CannonController.MuzzlePoint.position;
            m_AimController.SetAimTarget(muzzlePos + new Vector2(2f, -5f));
            yield return null;

            float angleFromUp = Vector2.Angle(Vector2.up, m_AimController.AimDirection);
            Assert.LessOrEqual(angleFromUp, m_Config.AimClampFromUpDeg + 1e-3f);
            Assert.Greater(m_AimController.AimDirection.y, 0f);
        }

        [UnityTest]
        public IEnumerator AimController_BallPathFromRotatedMuzzle_PassesThroughTarget()
        {
            // The muzzle swings around the head pivot: the ray the ball travels (rotated muzzle,
            // AimDirection) must pass through the tapped point, and the ✕ / sight line sit on it
            Vector2 target = new Vector2(3f, 6f);
            m_AimController.SetAimTarget(target);
            yield return null;

            AssertReticleOnBallPath();
            Vector2 muzzle = m_CannonController.MuzzlePoint.position;
            Vector2 toTarget = target - muzzle;
            float offPath = Mathf.Abs(toTarget.x * m_AimController.AimDirection.y - toTarget.y * m_AimController.AimDirection.x);
            Assert.Less(offPath, 1e-3f, "Ball path misses the aim point");
        }

        [UnityTest]
        public IEnumerator AimController_RailMoveThenRefresh_KeepsReticleAndReaims()
        {
            m_AimController.SetAimTarget(new Vector2(-2f, 8f));
            yield return null;
            Vector2 reticleBefore = m_AimController.ReticlePosition;

            m_CannonController.SetRailX(3f);
            m_AimController.RefreshAim();
            yield return null;

            Assert.AreEqual(reticleBefore.x, m_AimController.ReticlePosition.x, 1e-3f, "The ✕ stays put when the cannon slides");
            Assert.AreEqual(reticleBefore.y, m_AimController.ReticlePosition.y, 1e-3f, "The ✕ stays put when the cannon slides");
            AssertReticleOnBallPath();
        }

        [UnityTest]
        public IEnumerator AimController_ReticleClampedToArena_StaysOnBallPath()
        {
            // Far off to the side: the reticle is pulled back along the ray, never sideways
            m_AimController.SetAimTarget(new Vector2(20f, 12f));
            yield return null;

            Assert.LessOrEqual(Mathf.Abs(m_AimController.ReticlePosition.x), 4.8f + 1e-3f);
            AssertReticleOnBallPath();
        }

        private void AssertReticleOnBallPath()
        {
            Vector2 muzzle = m_CannonController.MuzzlePoint.position;
            Vector2 toReticle = (m_AimController.ReticlePosition - muzzle).normalized;
            Assert.Less(Vector2.Angle(toReticle, m_AimController.AimDirection), 0.01f, "✕ is off the ball's path");
        }

        [UnityTest]
        public IEnumerator AimController_ReticleHolding_ScalesUpAndDown()
        {
            m_AimController.SetReticleHolding(true);
            yield return null;
            Assert.AreEqual(1.25f, m_ReticleTransform.localScale.x, 1e-3f);

            m_AimController.SetReticleHolding(false);
            yield return null;
            Assert.AreEqual(1.0f, m_ReticleTransform.localScale.x, 1e-3f);
        }
    }
}
