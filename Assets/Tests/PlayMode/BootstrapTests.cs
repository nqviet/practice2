using System.Collections;
using Game.Runtime.GameFlow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class BootstrapTests
    {
        [UnityTest]
        public IEnumerator GameManager_Instantiates_AndSetsInstance()
        {
            GameObject go = new GameObject("GameManager");
            GameManager gm = go.AddComponent<GameManager>();

            yield return null;

            Assert.IsNotNull(GameManager.Instance);
            Assert.AreEqual(gm, GameManager.Instance);

            Object.Destroy(go);
            yield return null;
        }
    }
}
