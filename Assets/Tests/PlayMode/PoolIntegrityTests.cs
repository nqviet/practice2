using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Runtime.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class PoolIntegrityTests
    {
        private GameObject m_ServiceGo;
        private PoolService m_PoolService;
        private GameObject m_Prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_ServiceGo = new GameObject("TestPoolService");
            m_PoolService = m_ServiceGo.AddComponent<PoolService>();

            m_Prefab = new GameObject("TestPooledPrefab");
            m_Prefab.AddComponent<PooledInstance>();
            m_Prefab.SetActive(false);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (m_Prefab != null) Object.Destroy(m_Prefab);
            if (m_ServiceGo != null) Object.Destroy(m_ServiceGo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PoolService_PrewarmAndRecycle_ZeroRuntimeInstantiations()
        {
            const int prewarmCount = 5;
            m_PoolService.Prewarm(m_Prefab, prewarmCount);

            Assert.AreEqual(0, m_PoolService.GetActiveCount(m_Prefab));
            Assert.AreEqual(prewarmCount, m_PoolService.GetPooledCount(m_Prefab));
            Assert.AreEqual(0, m_PoolService.RuntimeInstantiateCount, "Prewarm should not count as runtime instantiate");

            // Perform 10 spawn/despawn cycles
            var activeList = new List<GameObject>();
            for (int cycle = 0; cycle < 10; cycle++)
            {
                // Spawn 3 instances
                for (int i = 0; i < 3; i++)
                {
                    GameObject instance = m_PoolService.Spawn(m_Prefab, Vector3.zero, Quaternion.identity);
                    Assert.IsNotNull(instance);
                    Assert.IsTrue(instance.activeSelf);
                    activeList.Add(instance);
                }

                Assert.AreEqual(3, m_PoolService.GetActiveCount(m_Prefab));
                Assert.AreEqual(2, m_PoolService.GetPooledCount(m_Prefab));
                Assert.AreEqual(0, m_PoolService.RuntimeInstantiateCount, "No runtime instantiations should happen when pool has capacity");

                // Despawn the 3 instances
                for (int i = 0; i < activeList.Count; i++)
                {
                    m_PoolService.Despawn(activeList[i]);
                    Assert.IsFalse(activeList[i].activeSelf);
                }
                activeList.Clear();

                Assert.AreEqual(0, m_PoolService.GetActiveCount(m_Prefab));
                Assert.AreEqual(prewarmCount, m_PoolService.GetPooledCount(m_Prefab));
                Assert.AreEqual(0, m_PoolService.RuntimeInstantiateCount);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator PoolService_ReleaseAll_DespawnsAllActiveInstances()
        {
            const int prewarmCount = 4;
            m_PoolService.Prewarm(m_Prefab, prewarmCount);

            for (int i = 0; i < prewarmCount; i++)
            {
                m_PoolService.Spawn(m_Prefab, Vector3.zero, Quaternion.identity);
            }

            Assert.AreEqual(prewarmCount, m_PoolService.GetActiveCount(m_Prefab));
            Assert.AreEqual(0, m_PoolService.GetPooledCount(m_Prefab));

            m_PoolService.ReleaseAll(m_Prefab);

            Assert.AreEqual(0, m_PoolService.GetActiveCount(m_Prefab));
            Assert.AreEqual(prewarmCount, m_PoolService.GetPooledCount(m_Prefab));

            yield return null;
        }
    }
}
