using System.Collections;
using Game.Board;
using Game.Core;
using Game.Runtime.Ball;
using Game.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class ShotLifecycleTests
    {
        private GameObject m_RootGo;
        private BallStream m_BallStream;
        private LevelController m_LevelController;
        private PoolService m_PoolService;
        private GameplayConfig m_Config;
        private Ball m_BallPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_RootGo = new GameObject("TestShotLifecycleRoot");

            m_PoolService = m_RootGo.AddComponent<PoolService>();
            m_LevelController = m_RootGo.AddComponent<LevelController>();
            m_BallStream = m_RootGo.AddComponent<BallStream>();

            // Setup Config
            m_Config = ScriptableObject.CreateInstance<GameplayConfig>();
            m_Config.BallSpeed = 10f;
            m_Config.ReturnMargin = 0.2f;
            m_Config.ShotTimeoutSec = 0.5f; // Short timeout for test

            // Setup Ball Prefab
            var ballGo = new GameObject("BallPrefab");
            ballGo.layer = GameConstants.BallLayer;
            m_BallPrefab = ballGo.AddComponent<Ball>();
            var rb = ballGo.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
            }
            ballGo.SetActive(false);

            m_BallStream.SetReferences(m_Config, m_BallPrefab, m_RootGo.transform, m_LevelController, m_PoolService);
            m_BallStream.LoadedKind = GameColor.Red;

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
        public IEnumerator ShotLifecycle_ReturnLine_RecallsBallAndReloadsSameKind()
        {
            bool returnedFired = false;
            m_BallStream.OnBallReturned = () => returnedFired = true;

            // Fire downwards toward return line (GameConstants.ReturnLineY = -0.30f)
            m_RootGo.transform.position = new Vector3(0f, 0.5f, 0f);
            m_BallStream.Fire(Vector2.down);

            Assert.IsTrue(m_BallStream.IsBallInFlight);
            Assert.IsNotNull(m_BallStream.CurrentBall);

            // Wait for ball to drop below ReturnLineY - margin
            float elapsed = 0f;
            while (!returnedFired && elapsed < 1.0f)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(returnedFired, "OnBallReturned should fire when ball drops below return line");
            Assert.IsFalse(m_BallStream.IsBallInFlight, "Ball should no longer be in flight");
            Assert.IsNull(m_BallStream.CurrentBall, "Current ball should be despawned and cleared");
            Assert.AreEqual(GameColor.Red, m_BallStream.LoadedKind, "Cannon should reload with same kind");
        }

        [UnityTest]
        public IEnumerator ShotLifecycle_Timeout_ForcesRecall()
        {
            bool returnedFired = false;
            m_BallStream.OnBallReturned = () => returnedFired = true;

            // Fire into deep space with return line far below
            m_BallStream.Fire(Vector2.up);
            Assert.IsTrue(m_BallStream.IsBallInFlight);

            // Wait beyond shotTimeoutSec (0.5s)
            yield return new WaitForSeconds(0.6f);

            Assert.IsTrue(returnedFired, "Ball should be force-recalled upon timeout");
            Assert.IsFalse(m_BallStream.IsBallInFlight);
            Assert.IsNull(m_BallStream.CurrentBall);
        }

        [UnityTest]
        public IEnumerator ShotLifecycle_MatchingBlockDetonation_ConsumesBall()
        {
            // Spawn a Red Colored Block in the ball's flight path
            var blockGo = new GameObject("TargetBlock");
            blockGo.transform.position = new Vector3(0f, 2.0f, 0f);
            blockGo.layer = GameConstants.BlockLayer;

            var col = blockGo.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            var blockDef = ScriptableObject.CreateInstance<BlockDefinition>();
            blockDef.Kind = BlockKind.Colored;
            blockDef.Color = GameColor.Red;

            var block = blockGo.AddComponent<Block>();
            block.Init(new Vector2Int(0, 0), blockDef);

            bool detonationHit = false;
            m_BallStream.OnDetonationHit = (hitBlock) => detonationHit = true;
            m_BallStream.LoadedKind = GameColor.Red;

            m_RootGo.transform.position = Vector3.zero;
            m_BallStream.Fire(Vector2.up);

            float elapsed = 0f;
            while (!detonationHit && elapsed < 1.0f)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Assert.IsTrue(detonationHit, "Detonation hit callback should fire when matching block is touched");
            Assert.IsFalse(m_BallStream.IsBallInFlight, "Ball should be consumed upon detonation");
            Assert.IsNull(m_BallStream.CurrentBall);
            Assert.AreEqual(GameColor.Red, m_BallStream.LoadedKind, "Cannon should reload same kind");

            Object.Destroy(blockGo);
            Object.Destroy(blockDef);
        }
    }
}
