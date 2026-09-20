using System.Collections;
using Game.Core;
using Game.Runtime.Ball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class BallConstantSpeedTests
    {
        private GameObject m_ArenaBox;
        private GameObject m_BallGo;
        private Ball m_Ball;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // Configure layers and collision
            Physics2D.IgnoreLayerCollision(GameConstants.BallLayer, GameConstants.WallLayer, false);

            // Create a small enclosed 2x2 arena box for rapid bouncing
            m_ArenaBox = new GameObject("TestArenaBox");

            CreateWall(m_ArenaBox, new Vector2(-1.5f, 0f), new Vector2(0.5f, 4f)); // Left
            CreateWall(m_ArenaBox, new Vector2(1.5f, 0f), new Vector2(0.5f, 4f));  // Right
            CreateWall(m_ArenaBox, new Vector2(0f, 1.5f), new Vector2(4f, 0.5f));  // Top
            CreateWall(m_ArenaBox, new Vector2(0f, -1.5f), new Vector2(4f, 0.5f)); // Bottom

            m_BallGo = new GameObject("TestBall");
            m_BallGo.layer = GameConstants.BallLayer;
            m_Ball = m_BallGo.AddComponent<Ball>();
            m_BallGo.transform.position = Vector3.zero;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_BallGo != null) Object.Destroy(m_BallGo);
            if (m_ArenaBox != null) Object.Destroy(m_ArenaBox);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ball_MaintainsConstantSpeed_AcrossFiftyBounces()
        {
            const float targetSpeed = 14.0f;
            int bounceCount = 0;
            m_Ball.OnBounced += () => bounceCount++;

            // ReturnLine is far below so ball won't return during this bounce test
            m_Ball.Configure(targetSpeed, -50f, 0.35f);
            m_Ball.Launch(new Vector2(1.1f, 0.9f).normalized, targetSpeed, GameColor.White);

            var rb = m_Ball.GetComponent<Rigidbody2D>();
            int maxFrames = 500;
            int framesRun = 0;

            while (bounceCount < 50 && framesRun < maxFrames)
            {
                yield return new WaitForFixedUpdate();
                framesRun++;

                float currentSpeed = rb.linearVelocity.magnitude;
                Assert.AreEqual(targetSpeed, currentSpeed, 1e-3f,
                    $"Ball speed drifted on frame {framesRun} after {bounceCount} bounces. Expected {targetSpeed}, was {currentSpeed}");
            }

            Assert.GreaterOrEqual(bounceCount, 50, "Ball should have completed at least 50 bounces within the test window");
        }

        [UnityTest]
        public IEnumerator Ball_FastForward_DoublesSpeedWithoutDrift()
        {
            const float baseSpeed = 14.0f;
            const float multiplier = 2.0f;
            const float expectedSpeed = baseSpeed * multiplier;

            m_Ball.Configure(baseSpeed, -50f, 0.35f);
            m_Ball.Launch(new Vector2(1.0f, 0.7f).normalized, baseSpeed, GameColor.White);
            m_Ball.SetFastForward(true, multiplier);

            var rb = m_Ball.GetComponent<Rigidbody2D>();

            for (int i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                float currentSpeed = rb.linearVelocity.magnitude;
                Assert.AreEqual(expectedSpeed, currentSpeed, 1e-3f,
                    $"Fast-forward ball speed drifted on frame {i}. Expected {expectedSpeed}, was {currentSpeed}");
            }
        }

        [UnityTest]
        public IEnumerator Ball_CornerReflection_ProducesCleanBounce()
        {
            // Position ball to hit corner directly at 45 degrees
            m_BallGo.transform.position = new Vector2(0.5f, 0.5f);
            m_Ball.Configure(10.0f, -50f, 0.35f);

            int bounceCount = 0;
            m_Ball.OnBounced += () => bounceCount++;

            // Fire toward corner at (-1.25, -1.25)
            Vector2 toCorner = new Vector2(-1f, -1f).normalized;
            m_Ball.Launch(toCorner, 10.0f, GameColor.White);

            for (int i = 0; i < 20; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            // Ball should have bounced and be moving away from the corner
            var rb = m_Ball.GetComponent<Rigidbody2D>();
            Assert.AreEqual(10.0f, rb.linearVelocity.magnitude, 1e-3f);
            Assert.Greater(bounceCount, 0, "Ball should have bounced off the corner");
        }

        private static GameObject CreateWall(GameObject parent, Vector2 pos, Vector2 size)
        {
            var wall = new GameObject("Wall");
            wall.transform.SetParent(parent.transform);
            wall.transform.position = pos;
            wall.layer = GameConstants.WallLayer;

            var col = wall.AddComponent<BoxCollider2D>();
            col.size = size;

            return wall;
        }
    }
}
