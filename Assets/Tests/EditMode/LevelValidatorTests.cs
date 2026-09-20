using System.Collections.Generic;
using System.Linq;
using Game.Board;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class LevelValidatorTests
    {
        [Test]
        public void Rejects_UncoveredBrick()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Test_Uncovered";

            // Row 12 has a brick at (0, 12), but colored block is at (5, 5) - far out of 3x3 ring!
            string[] rows = new string[13];
            for (int i = 0; i < 13; i++) rows[i] = "..........";
            rows[0] = "N........."; // Row 12 has brick at col 0
            rows[7] = ".....R...."; // Row 5 has red block at col 5

            level.Rows = rows;

            ValidationReport report = LevelValidator.Validate(level);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.UncoveredBricks.Count > 0);
            Assert.AreEqual(new Vector2Int(0, 12), report.UncoveredBricks[0]);
        }

        [Test]
        public void Rejects_SealedColoredBlock()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Test_Sealed";

            // Colored block completely sealed behind steel blocks (no 4-way empty neighbor)
            string[] rows = new string[13];
            for (int i = 0; i < 13; i++) rows[i] = "..........";

            // At row 5, col 5: Red block surrounded 4-way by Steel
            // Row 6 (index 6): col 5 is S
            // Row 5 (index 7): col 4 is S, col 5 is R, col 6 is S
            // Row 4 (index 8): col 5 is S
            rows[6] = ".....S....";
            rows[7] = "....SRS...";
            rows[8] = ".....S....";

            level.Rows = rows;

            ValidationReport report = LevelValidator.Validate(level);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.UnreachableColored.Count > 0);
            Assert.AreEqual(new Vector2Int(5, 5), report.UnreachableColored[0]);
        }

        [Test]
        public void Accepts_SealedColoredBlock_RescuedBySameColorChain()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Test_ChainRescue";

            string[] rows = new string[13];
            for (int i = 0; i < 13; i++) rows[i] = "..........";

            // Red at (5,5) is sealed 4-way by steel, BUT has an 8-way neighbor at (6,6)
            // which is also Red and IS 4-way accessible to empty cells!
            rows[6] = ".....SR..."; // (5,6) is Steel, (6,6) is Red (accessible to empty right)
            rows[7] = "....SRS..."; // (4,5) is Steel, (5,5) is Red, (6,5) is Steel
            rows[8] = ".....S...."; // (5,4) is Steel

            level.Rows = rows;

            ValidationReport report = LevelValidator.Validate(level);
            // Neither Red block should be marked unreachable
            Assert.AreEqual(0, report.UnreachableColored.Count);
        }

        [Test]
        public void Rejects_ReservedCharacters_M_And_X()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Test_Reserved";

            string[] rows = new string[13];
            for (int i = 0; i < 13; i++) rows[i] = "..........";
            rows[0] = "M....X....";

            level.Rows = rows;

            ValidationReport report = LevelValidator.Validate(level);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("reserved kind")));
        }

        [Test]
        public void Rejects_MalformedRows()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "Test_Malformed";

            // Row with only 5 characters instead of 10
            string[] rows = new string[13];
            for (int i = 0; i < 13; i++) rows[i] = "..........";
            rows[0] = ".....";

            level.Rows = rows;

            ValidationReport report = LevelValidator.Validate(level);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("must have exactly 10 characters")));
        }

        [Test]
        public void BallClearance_ValidatesRadiusInvariant()
        {
            // ballRadius <= 0.35 and 2 * radius < 0.90
            Assert.IsTrue(LevelValidator.ValidateBallClearance(0.28f, out _));
            Assert.IsTrue(LevelValidator.ValidateBallClearance(0.35f, out _));

            // Exceeds 0.35 invariant
            Assert.IsFalse(LevelValidator.ValidateBallClearance(0.36f, out string err1));
            StringAssert.Contains("clearance invariant", err1);

            // Large radius violating diameter clearance
            Assert.IsFalse(LevelValidator.ValidateBallClearance(0.50f, out string err2));
            StringAssert.Contains("clearance invariant", err2);
        }
    }
}
