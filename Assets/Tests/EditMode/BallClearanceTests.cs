using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class BallClearanceTests
    {
        [Test]
        public void BallClearance_InvariantSatisfied_PassesThroughOneCellCracks()
        {
            // From technical_design.md §4.3:
            // "A 1-unit cell with a full-cell block collider leaves a 1-unit gap,
            // so with ballRadius = 0.28 (diameter 0.56) there is 0.44 u of clearance.
            // Assert 2 * ballRadius < 1f - 0.1f"
            var config = ScriptableObject.CreateInstance<GameplayConfig>();
            float ballRadius = config.BallRadius;

            Assert.LessOrEqual(ballRadius, 0.35f, "GameplayConfig.ballRadius must be <= 0.35f");
            Assert.Less(2f * ballRadius, 1.0f - 0.1f, "2 * ballRadius must be < 1.0f - 0.1f for level solvability");
            Assert.LessOrEqual(config.BallSpeed, 60.0f, "Ball speed must not exceed 60 u/s max safe limit at 60 Hz");
            Assert.Greater(config.FastForwardMultiplier, 1.0f, "Fast forward multiplier must be greater than 1");
            Assert.AreEqual(5, config.MaxTrailRuns, "Max trail runs must be 5 per spec");

            Object.DestroyImmediate(config);
        }
    }
}
