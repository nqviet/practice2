using System.Collections.Generic;
using Game.Board;
using Game.Core;
using Game.Runtime.Objectives;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class ObjectiveTrackerTests
    {
        private GameObject m_TestObject;
        private ObjectiveTracker m_Tracker;

        [SetUp]
        public void SetUp()
        {
            m_TestObject = new GameObject("Test_ObjectiveTracker");
            m_Tracker = m_TestObject.AddComponent<ObjectiveTracker>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestObject != null)
            {
                Object.DestroyImmediate(m_TestObject);
            }
        }

        [Test]
        public void ObjectiveTracker_ResetTracker_InitializesRemainingBricks()
        {
            int reportedCount = -1;
            m_Tracker.OnBricksRemainingChanged += count => reportedCount = count;

            m_Tracker.ResetTracker(40);

            Assert.AreEqual(40, m_Tracker.BricksRemaining);
            Assert.IsFalse(m_Tracker.HasWon);
            Assert.AreEqual(40, reportedCount);
        }

        [Test]
        public void ObjectiveTracker_HandleDetonation_DecrementsBricksRemaining()
        {
            m_Tracker.ResetTracker(10);

            var destroyedList = new List<Vector2Int> { new Vector2Int(0, 0) };
            var depthMap = new Dictionary<Vector2Int, int>();
            var result = new ChainResult(destroyedList, depthMap, 4, 0);

            var method = typeof(ObjectiveTracker).GetMethod("HandleDetonation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method);
            method.Invoke(m_Tracker, new object[] { result });

            Assert.AreEqual(6, m_Tracker.BricksRemaining);
            Assert.IsFalse(m_Tracker.HasWon);
        }

        [Test]
        public void ObjectiveTracker_ReachingZeroBricks_TriggersWinEventOnce()
        {
            m_Tracker.ResetTracker(3);

            bool winTriggered = false;
            int winTriggerCount = 0;
            m_Tracker.OnWin += () =>
            {
                winTriggered = true;
                winTriggerCount++;
            };

            var destroyedList = new List<Vector2Int>();
            var depthMap = new Dictionary<Vector2Int, int>();
            var result = new ChainResult(destroyedList, depthMap, 3, 0);

            var method = typeof(ObjectiveTracker).GetMethod("HandleDetonation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(m_Tracker, new object[] { result });

            Assert.IsTrue(winTriggered);
            Assert.AreEqual(1, winTriggerCount);
            Assert.AreEqual(0, m_Tracker.BricksRemaining);
            Assert.IsTrue(m_Tracker.HasWon);

            // Additional detonation after win should not re-trigger win
            method.Invoke(m_Tracker, new object[] { result });
            Assert.AreEqual(1, winTriggerCount);
        }

        [Test]
        public void ObjectiveTracker_Overkill_ClampsToZero()
        {
            m_Tracker.ResetTracker(2);

            var destroyedList = new List<Vector2Int>();
            var depthMap = new Dictionary<Vector2Int, int>();
            var result = new ChainResult(destroyedList, depthMap, 5, 0);

            var method = typeof(ObjectiveTracker).GetMethod("HandleDetonation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(m_Tracker, new object[] { result });

            Assert.AreEqual(0, m_Tracker.BricksRemaining);
            Assert.IsTrue(m_Tracker.HasWon);
        }
    }
}
