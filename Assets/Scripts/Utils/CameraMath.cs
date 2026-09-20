using Game.Core;
using UnityEngine;

namespace Game.Utils
{
    public static class CameraMath
    {
        public const float DefaultArenaWidth = 10.8f;
        public const float DefaultMinOrthoSize = 8.85f;
        public const float DefaultGridTop = 13.0f;
        // Head-room above the grid for the HUD TopBar (gear, "LEVEL N", restart, legend).
        // 1.8 u ≈ the mockup's header (grid starts ~1.7 cells below the screen top); the
        // original 0.6 u left ~60 px, so the TopBar covered rows 11-12.
        public const float DefaultTopMargin = 1.8f;
        public const float DefaultCannonDropLimit = 2.5f;
        public const float MinCannonClearanceBelowReturnLine = 1.0f;

        public static float CalculateOrthoSize(float aspect, float minOrthoSize = DefaultMinOrthoSize, float arenaWidth = DefaultArenaWidth)
        {
            if (aspect <= 0f) aspect = 9f / 16f;
            float widthDrivenOrtho = (arenaWidth * 0.5f) / aspect;
            return Mathf.Max(widthDrivenOrtho, minOrthoSize);
        }

        public static float CalculateCameraY(float orthoSize, float gridTop = DefaultGridTop, float topMargin = DefaultTopMargin)
        {
            return gridTop + topMargin - orthoSize;
        }

        public static float CalculateScreenBottom(float camY, float orthoSize)
        {
            return camY - orthoSize;
        }

        public static float CalculateCannonY(
            float screenBottom,
            float bannerUnits,
            float cannonBottomPad,
            float returnLineY = GameConstants.ReturnLineY,
            float maxDropFromReturnLine = DefaultCannonDropLimit)
        {
            float rawCannonY = screenBottom + bannerUnits + cannonBottomPad;
            // R2 Cap: on tall devices, cap the drop so cannon does not fall far below return line
            float cappedCannonY = Mathf.Max(rawCannonY, returnLineY - maxDropFromReturnLine);
            // Invariant: cannon must never intersect the return line
            float maxAllowedCannonY = returnLineY - MinCannonClearanceBelowReturnLine;
            return Mathf.Min(cappedCannonY, maxAllowedCannonY);
        }

        public static float FrustumHalfWidth(float orthoSize, float aspect)
        {
            return orthoSize * aspect;
        }

        public static float FrustumLeft(float camX, float orthoSize, float aspect)
        {
            return camX - FrustumHalfWidth(orthoSize, aspect);
        }

        public static float FrustumRight(float camX, float orthoSize, float aspect)
        {
            return camX + FrustumHalfWidth(orthoSize, aspect);
        }

        public static float FrustumTop(float camY, float orthoSize)
        {
            return camY + orthoSize;
        }

        public static float FrustumBottom(float camY, float orthoSize)
        {
            return camY - orthoSize;
        }
    }
}
