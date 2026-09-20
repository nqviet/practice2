using Game.Core;
using Game.Utils;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class CameraFitterTests
    {
        [Test]
        public void CameraContract_AspectSweep_TenColumnsAlwaysInsideFrustum()
        {
            // Grid columns span x in [-5.0, 5.0] (10 columns centered around 0)
            const float gridMinX = -5.0f;
            const float gridMaxX = 5.0f;

            // Aspect sweep from 0.42 to 0.62 (e.g. 21:9 up to 16:10 / 4:3 portrait ratios)
            for (float aspect = 0.42f; aspect <= 0.62f; aspect += 0.01f)
            {
                float orthoSize = CameraMath.CalculateOrthoSize(aspect);
                float frustumLeft = CameraMath.FrustumLeft(0f, orthoSize, aspect);
                float frustumRight = CameraMath.FrustumRight(0f, orthoSize, aspect);

                Assert.LessOrEqual(frustumLeft, gridMinX,
                    $"At aspect {aspect:F2}, frustum left ({frustumLeft}) should encompass gridMinX ({gridMinX})");
                Assert.GreaterOrEqual(frustumRight, gridMaxX,
                    $"At aspect {aspect:F2}, frustum right ({frustumRight}) should encompass gridMaxX ({gridMaxX})");
            }
        }

        [Test]
        public void CameraContract_AspectSweep_GridTopAlwaysPinned()
        {
            const float expectedPinnedTop = CameraMath.DefaultGridTop + CameraMath.DefaultTopMargin; // 14.8f

            for (float aspect = 0.42f; aspect <= 0.62f; aspect += 0.01f)
            {
                float orthoSize = CameraMath.CalculateOrthoSize(aspect);
                float camY = CameraMath.CalculateCameraY(orthoSize);
                float frustumTop = CameraMath.FrustumTop(camY, orthoSize);

                Assert.AreEqual(expectedPinnedTop, frustumTop, 1e-4f,
                    $"At aspect {aspect:F2}, visible top should be strictly pinned at {expectedPinnedTop}");
            }
        }

        [Test]
        public void CameraContract_AspectSweep_CannonNeverIntersectsReturnLine()
        {
            float returnLineY = GameConstants.ReturnLineY;
            float cannonBottomPad = 0.35f;

            // Sweep aspects and banner units
            float[] bannerUnitsToTest = new[] { 0.0f, 0.5f, 1.0f, 1.5f, 2.0f };

            foreach (float bannerUnits in bannerUnitsToTest)
            {
                for (float aspect = 0.42f; aspect <= 0.62f; aspect += 0.01f)
                {
                    float orthoSize = CameraMath.CalculateOrthoSize(aspect);
                    float camY = CameraMath.CalculateCameraY(orthoSize);
                    float screenBottom = CameraMath.CalculateScreenBottom(camY, orthoSize);
                    float cannonY = CameraMath.CalculateCannonY(screenBottom, bannerUnits, cannonBottomPad, returnLineY);

                    Assert.Less(cannonY, returnLineY,
                        $"At aspect {aspect:F2} and banner {bannerUnits}, cannonY ({cannonY}) must be strictly below returnLineY ({returnLineY})");

                    // Must have at least 1.0 clearance below return line
                    Assert.LessOrEqual(cannonY, returnLineY - 1.0f,
                        $"At aspect {aspect:F2} and banner {bannerUnits}, cannonY ({cannonY}) must maintain safe clearance below return line");
                }
            }
        }

        [Test]
        public void CameraContract_TallAspect_CannonCapEngages()
        {
            // On very tall devices (aspect = 0.45, e.g. 20:9), raw screenBottom is very low
            float aspect = 0.45f;
            float bannerUnits = 0.0f;
            float cannonBottomPad = 0.35f;
            float returnLineY = GameConstants.ReturnLineY; // -0.30f

            float orthoSize = CameraMath.CalculateOrthoSize(aspect);
            float camY = CameraMath.CalculateCameraY(orthoSize);
            float screenBottom = CameraMath.CalculateScreenBottom(camY, orthoSize);
            float rawCannonY = screenBottom + bannerUnits + cannonBottomPad;

            float cannonY = CameraMath.CalculateCannonY(screenBottom, bannerUnits, cannonBottomPad, returnLineY);

            // Raw cannonY is far below ReturnLineY - 2.5f
            Assert.Less(rawCannonY, returnLineY - 2.5f);
            // Capped cannonY is clamped at ReturnLineY - 2.5f (-2.80f)
            Assert.AreEqual(returnLineY - 2.5f, cannonY, 1e-4f);
        }
    }
}
