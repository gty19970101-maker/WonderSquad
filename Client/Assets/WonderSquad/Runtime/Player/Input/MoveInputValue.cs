using UnityEngine;

namespace WonderSquad.Player.Input
{
    /// <summary>
    /// Applies the shared safety rules for two-dimensional player input.
    /// </summary>
    public static class MoveInputValue
    {
        private const float ClampPrecisionMargin = 0.000001f;
        private const float MaximumMagnitude = 1f;

        public static Vector2 Clamp(Vector2 rawInput)
        {
            if (!IsFinite(rawInput.x) || !IsFinite(rawInput.y))
            {
                return Vector2.zero;
            }

            var clampedInput =
                Vector2.ClampMagnitude(rawInput, MaximumMagnitude);
            if (clampedInput.sqrMagnitude <=
                MaximumMagnitude * MaximumMagnitude)
            {
                return clampedInput;
            }

            return clampedInput.normalized *
                (MaximumMagnitude - ClampPrecisionMargin);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
