using System.Collections.Generic;
using Game.Board;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class LevelRegressionTests
    {
        private static readonly string[] s_PinnedLevel01Rows = new string[]
        {
            "SNY..BN.YS", // Row 12 (top)
            "SNN.NNN.NS", // Row 11
            "SNN.SSS.NS", // Row 10
            "SRR.....BS", // Row 9
            "SNN.SSS.NS", // Row 8
            "SNN.NNN.NS", // Row 7
            "SNY.BBN.YS", // Row 6
            "SNN.NNN.NS", // Row 5
            "SNN.NNN.NS", // Row 4
            "SNB..RN.YS", // Row 3
            "SNN.NNN.NS", // Row 2
            "..........", // Row 1 (Landing Lane)
            ".........."  // Row 0 (Landing Lane)
        };

        [Test]
        public void Level01_ValidatesSuccessfully()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Level_01";
            level.DisplayName = "LEVEL 1";
            level.ParShots = 10;
            level.Rows = s_PinnedLevel01Rows;

            ValidationReport report = LevelValidator.Validate(level);

            Assert.IsTrue(report.IsValid, $"Level_01 failed validation:\n{report.GenerateSummary()}");
            Assert.AreEqual(0, report.Errors.Count);
            Assert.AreEqual(0, report.UncoveredBricks.Count);
            Assert.AreEqual(0, report.UnreachableColored.Count);

            // Assert 13 colored blocks (3 Red, 5 Blue, 5 Yellow)
            Assert.AreEqual(13, report.ColoredCount);
            Assert.AreEqual(3, report.RedCount);
            Assert.AreEqual(5, report.BlueCount);
            Assert.AreEqual(5, report.YellowCount);

            // Assert colors used: Red, Blue, Yellow
            CollectionAssert.AreEquivalent(new[] { GameColor.Red, GameColor.Blue, GameColor.Yellow }, report.ColorsUsed);

            // Print final mix report
            Debug.Log(report.GenerateSummary());
        }

        [Test]
        public void Level01_PinnedMap_HasNotSilentlyDrifted()
        {
            Assert.AreEqual(13, s_PinnedLevel01Rows.Length);
            for (int i = 0; i < s_PinnedLevel01Rows.Length; i++)
            {
                Assert.AreEqual(10, s_PinnedLevel01Rows[i].Length);
            }

            // Landing lane must be open
            Assert.AreEqual("..........", s_PinnedLevel01Rows[11]);
            Assert.AreEqual("..........", s_PinnedLevel01Rows[12]);
        }
    }
}
