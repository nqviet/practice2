using System.Collections;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// Regressions found in playtest: the ball must leave the muzzle (which sits below the return
    /// line) without being recalled, and a flat wall built from flush block colliders must reflect
    /// like a mirror even when the ball lands on the seam between two blocks.
    /// </summary>
    [TestFixture]
    public class BallBounceRegressionTests
    {
        private const float Speed = 14.0f;

        private GameObject m_Root;
        private Ball m_Ball;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Physics2D.IgnoreLayerCollision(GameConstants.BallLayer, GameConstants.BlockLayer, false);

            m_Root = new GameObject("TestBallBounceRoot");

            var ballGo = new GameObject("TestBall");
            ballGo.transform.SetParent(m_Root.transform);
            ballGo.layer = GameConstants.BallLayer;
            m_Ball = ballGo.AddComponent<Ball>();

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Root != null) Object.Destroy(m_Root);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ball_LaunchedUpFromBelowReturnLine_StaysInFlight()
        {
            bool returned = false;
            m_Ball.OnReturned += () => returned = true;

            // Muzzle height in 02_Gameplay: below ReturnLineY - margin
            m_Ball.transform.position = new Vector3(0f, -0.77f, 0f);
            m_Ball.Configure(Speed, GameConstants.ReturnLineY, 0.35f);
            m_Ball.Launch(Vector2.up, Speed, GameColor.Red);

            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsFalse(returned, "A ball rising out of the muzzle must not count as returned");
            Assert.IsTrue(m_Ball.IsInFlight);
        }

        [UnityTest]
        public IEnumerator Ball_DescendingBelowReturnLine_Returns()
        {
            bool returned = false;
            m_Ball.OnReturned += () => returned = true;

            m_Ball.transform.position = new Vector3(0f, 0.5f, 0f);
            m_Ball.Configure(Speed, GameConstants.ReturnLineY, 0.35f);
            m_Ball.Launch(Vector2.down, Speed, GameColor.Red);

            for (int i = 0; i < 30 && !returned; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(returned, "A falling ball must still end the shot at the return line");
        }

        [UnityTest]
        public IEnumerator Ball_HitsSeamOfFlatBlockWall_ReflectsLikeMirror()
        {
            // Two flush full-cell blocks forming one flat face at x = -2 with a seam at y = 5
            CreateBlock(new Vector2(-2.5f, 4.5f));
            CreateBlock(new Vector2(-2.5f, 5.5f));
            yield return new WaitForFixedUpdate();

            Vector2 dir = new Vector2(-2.66f, 13.75f).normalized;
            const float radius = 0.28f;
            const float faceX = -2f;

            // Aim the contact point at, and just around, the seam
            for (float contactY = 4.85f; contactY <= 5.151f; contactY += 0.05f)
            {
                float travelY = 3f;
                var start = new Vector2(faceX + radius - dir.x / dir.y * travelY, contactY - travelY);

                m_Ball.Stop();
                m_Ball.transform.position = start;
                var rb = m_Ball.GetComponent<Rigidbody2D>();
                rb.position = start;
                m_Ball.Configure(Speed, -50f, 0.35f);
                m_Ball.Launch(dir, Speed, GameColor.Red);

                bool bounced = false;
                m_Ball.OnBounced = () => bounced = true;

                for (int i = 0; i < 40 && !bounced; i++)
                {
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForFixedUpdate();

                Assert.IsTrue(bounced, $"Ball should hit the wall (contact y {contactY:F2})");
                Vector2 v = rb.linearVelocity;
                Vector2 expected = new Vector2(-dir.x, dir.y) * Speed;
                Assert.AreEqual(expected.x, v.x, 0.05f, $"Seam bounce skewed x (contact y {contactY:F2}): {v}");
                Assert.AreEqual(expected.y, v.y, 0.05f, $"Seam bounce skewed y (contact y {contactY:F2}): {v}");
            }
        }

        private void CreateBlock(Vector2 center)
        {
            var go = new GameObject("TestBlock");
            go.transform.SetParent(m_Root.transform);
            go.transform.position = center;
            go.layer = GameConstants.BlockLayer;
            go.AddComponent<Block>().GetComponent<BoxCollider2D>().size = Vector2.one;
        }
    }
}
