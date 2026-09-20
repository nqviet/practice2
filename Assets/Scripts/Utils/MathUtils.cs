using UnityEngine;

namespace Game.Utils
{
    public static class MathUtils
    {
        public static Vector2 ReflectAndNormalize(Vector2 incomingVelocity, Vector2 normal, float targetSpeed)
        {
            Vector2 reflected = Vector2.Reflect(incomingVelocity, normal);
            if (reflected.sqrMagnitude > 1e-6f)
            {
                return reflected.normalized * targetSpeed;
            }

            return normal * targetSpeed;
        }

        public static bool Approximately(float a, float b, float tolerance = 1e-3f)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }
    }
}
