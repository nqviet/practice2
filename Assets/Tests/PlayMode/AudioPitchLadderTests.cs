using Game.Runtime.Services;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.PlayMode
{
    [TestFixture]
    public class AudioPitchLadderTests
    {
        [Test]
        public void AudioPitchLadder_DepthZero_IsRootPitch()
        {
            float pitch = AudioService.CalculatePitch(0);
            Assert.AreEqual(1.0f, pitch, 0.001f);
        }

        [Test]
        public void AudioPitchLadder_AscendsMonotonicallyWithDepth()
        {
            float prevPitch = 0f;
            for (int depth = 0; depth <= 6; depth++)
            {
                float currentPitch = AudioService.CalculatePitch(depth);
                Assert.Greater(currentPitch, prevPitch);
                prevPitch = currentPitch;
            }
        }

        [Test]
        public void AudioPitchLadder_DepthSix_IsExactOctave()
        {
            // 6 whole steps = 12 semitones = 2.0x pitch
            float octavePitch = AudioService.CalculatePitch(6);
            Assert.AreEqual(2.0f, octavePitch, 0.001f);
        }

        [Test]
        public void AudioPitchLadder_SemitoneRatios_FollowEqualTemperament()
        {
            // Each depth step is 2 semitones (whole tone = 2^(2/12) ≈ 1.122462)
            float expectedStepRatio = Mathf.Pow(2.0f, 2f / 12f);
            for (int depth = 1; depth <= 5; depth++)
            {
                float ratio = AudioService.CalculatePitch(depth) / AudioService.CalculatePitch(depth - 1);
                Assert.AreEqual(expectedStepRatio, ratio, 0.001f);
            }
        }
    }
}
