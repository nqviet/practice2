using System.IO;
using Game.Core;
using Game.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class SaveServiceTests
    {
        private string m_Path;

        [SetUp]
        public void SetUp()
        {
            m_Path = Path.Combine(Application.temporaryCachePath, $"save_test_{System.Guid.NewGuid():N}.json");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (string file in new[] { m_Path, m_Path + ".tmp", m_Path + ".corrupt" })
            {
                if (File.Exists(file)) File.Delete(file);
            }
        }

        [Test]
        public void SaveThenLoad_RoundTripsProgressSettingsAndRecords()
        {
            var save = new SaveService(m_Path);
            save.Load();
            save.CurrentLevelIndex = 2;
            save.HighestUnlockedLevel = 3;
            save.Data.SfxEnabled = false;
            save.Data.MusicVolume = 0.25f;
            save.Data.GetOrCreateRecord("Level_01").RegisterCompletion(12);
            save.Save();

            var reloaded = new SaveService(m_Path);
            reloaded.Load();

            Assert.AreEqual(2, reloaded.CurrentLevelIndex);
            Assert.AreEqual(3, reloaded.HighestUnlockedLevel);
            Assert.IsFalse(reloaded.Data.SfxEnabled);
            Assert.AreEqual(0f, reloaded.Data.EffectiveSfxVolume);
            Assert.AreEqual(0.25f, reloaded.Data.MusicVolume, 1e-4f);
            Assert.AreEqual(12, reloaded.Data.GetRecord("Level_01").BestShots);
            Assert.IsFalse(File.Exists(m_Path + ".tmp"), "temp file must be cleaned up after an atomic write");
        }

        [Test]
        public void LevelRecord_KeepsBestShotsAndCountsCompletions()
        {
            var record = new LevelRecord("Level_01");

            Assert.IsTrue(record.RegisterCompletion(15));
            Assert.IsTrue(record.RegisterCompletion(10));
            Assert.IsFalse(record.RegisterCompletion(12));

            Assert.AreEqual(10, record.BestShots);
            Assert.AreEqual(3, record.Completions);
        }

        [Test]
        public void Load_CorruptFile_FallsBackToDefaultsAndKeepsBackup()
        {
            File.WriteAllText(m_Path, "{ this is not json");

            var save = new SaveService(m_Path);
            save.Load();

            Assert.AreEqual(0, save.CurrentLevelIndex);
            Assert.IsTrue(save.Data.MusicEnabled);
            Assert.IsTrue(File.Exists(m_Path + ".corrupt"));
        }

        [Test]
        public void InMemoryService_NeverTouchesDisk()
        {
            var save = new SaveService(null);
            save.Load();
            save.CurrentLevelIndex = 5;
            save.Save();

            Assert.IsFalse(save.IsPersistent);
            Assert.AreEqual(5, save.CurrentLevelIndex);
        }
    }
}
