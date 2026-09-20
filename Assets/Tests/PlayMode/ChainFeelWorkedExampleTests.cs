using System.Collections;
using System.Collections.Generic;
using Game.Board;
using Game.Core;
using Game.Runtime.Objectives;
using Game.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class ChainFeelWorkedExampleTests
    {
        private GameObject m_RootGo;
        private LevelController m_LevelController;
        private ObjectiveTracker m_ObjectiveTracker;
        private AudioService m_AudioService;
        private VFXService m_VFXService;
        private PoolService m_PoolService;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_RootGo = new GameObject("TestChainWorkedExampleRoot");

            m_PoolService = m_RootGo.AddComponent<PoolService>();
            m_AudioService = m_RootGo.AddComponent<AudioService>();
            m_VFXService = m_RootGo.AddComponent<VFXService>();
            m_VFXService.SetReferences(m_PoolService);

            m_LevelController = m_RootGo.AddComponent<LevelController>();
            m_ObjectiveTracker = m_RootGo.AddComponent<ObjectiveTracker>();
            m_ObjectiveTracker.SetReferences(m_LevelController);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_RootGo != null)
            {
                Object.Destroy(m_RootGo);
            }
            yield return null;
        }

        [Test]
        public void WorkedExample_ChainSolver_MatchesGameplaySpec()
        {
            // From gameplay.md §3:
            // When left Red (1,1) is hit by Red ball:
            // Detonates both adjacent Reds, destroying 6 bricks.
            var board = new BoardState();

            // Set Row 2: 2 bricks
            board.SetCell(new Vector2Int(1, 2), BlockKind.Brick, GameColor.None);
            board.SetCell(new Vector2Int(2, 2), BlockKind.Brick, GameColor.None);

            // Set Row 1: 2 bricks + 2 adjacent reds
            board.SetCell(new Vector2Int(0, 1), BlockKind.Brick, GameColor.None);
            board.SetCell(new Vector2Int(1, 1), BlockKind.Colored, GameColor.Red);
            board.SetCell(new Vector2Int(2, 1), BlockKind.Colored, GameColor.Red);
            board.SetCell(new Vector2Int(3, 1), BlockKind.Brick, GameColor.None);

            // Set Row 0: 2 bricks
            board.SetCell(new Vector2Int(1, 0), BlockKind.Brick, GameColor.None);
            board.SetCell(new Vector2Int(2, 0), BlockKind.Brick, GameColor.None);

            Vector2Int leftRed = new Vector2Int(1, 1);
            Vector2Int rightRed = new Vector2Int(2, 1);

            ChainResult result = ChainSolver.Solve(board, leftRed, GameColor.Red);

            Assert.AreEqual(2, result.ColoredDestroyed, "Both red blocks should detonate.");
            Assert.AreEqual(6, result.BricksDestroyed, "Exactly 6 bricks should be destroyed (§3).");
            Assert.IsTrue(result.DepthOf.ContainsKey(leftRed));
            Assert.IsTrue(result.DepthOf.ContainsKey(rightRed));
            Assert.AreEqual(0, result.DepthOf[leftRed]);
            Assert.AreEqual(1, result.DepthOf[rightRed]);
        }

        [UnityTest]
        public IEnumerator WorkedExample_DetonationRoutine_DrivesObjectiveAndSettleTokens()
        {
            var board = new BoardState();
            Vector2Int leftRed = new Vector2Int(1, 1);
            Vector2Int rightRed = new Vector2Int(2, 1);

            board.SetCell(leftRed, BlockKind.Colored, GameColor.Red);
            board.SetCell(rightRed, BlockKind.Colored, GameColor.Red);
            board.SetCell(new Vector2Int(1, 2), BlockKind.Brick, GameColor.None);
            board.SetCell(new Vector2Int(2, 2), BlockKind.Brick, GameColor.None);

            m_LevelController.SetBoardStateForTest(board);
            m_ObjectiveTracker.ResetTracker(2);

            ChainResult result = ChainSolver.Solve(board, leftRed, GameColor.Red);
            Assert.AreEqual(2, result.BricksDestroyed);

            // Trigger detonation playback on LevelController
            m_LevelController.ProcessDetonationResult(result);

            // Settle token should be retained during routine
            Assert.IsTrue(m_VFXService.IsSettling, "VFXService should be settling during detonation playback.");

            // Wait for 40ms freeze + 50ms stagger + collapse to complete (~0.3s unscaled)
            float timeout = 2.0f;
            float elapsed = 0f;
            while (m_VFXService.IsSettling && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsFalse(m_VFXService.IsSettling, "Settle token should be released upon completion.");
            Assert.IsTrue(m_ObjectiveTracker.HasWon, "ObjectiveTracker should register win when all bricks cleared.");
            Assert.AreEqual(0, m_ObjectiveTracker.BricksRemaining);
        }
    }
}
