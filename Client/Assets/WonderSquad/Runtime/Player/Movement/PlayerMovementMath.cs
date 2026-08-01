using UnityEngine;
using WonderSquad.Player.Input;

namespace WonderSquad.Player.Movement
{
    /// <summary>
    /// Provides deterministic calculations shared by local and future network
    /// movement drivers without reading an input device.
    /// </summary>
    public static class PlayerMovementMath
    {
        public static Vector3 CalculateWorldDirection(Vector2 moveInput)
        {
            var safeInput = MoveInputValue.Clamp(moveInput);
            return new Vector3(safeInput.x, 0f, safeInput.y);
        }

        public static Vector3 CalculateHorizontalVelocity(
            Vector2 moveInput,
            float maximumSpeed)
        {
            var safeMaximumSpeed = Mathf.Max(0f, maximumSpeed);
            return CalculateWorldDirection(moveInput) * safeMaximumSpeed;
        }

        public static float CalculateVerticalVelocity(
            float currentVerticalVelocity,
            bool isGrounded,
            float deltaTime,
            MovementSettings settings)
        {
            if (isGrounded && currentVerticalVelocity <= 0f)
            {
                return settings.GroundedVerticalSpeed;
            }

            var nextVerticalVelocity =
                currentVerticalVelocity +
                settings.GravityAcceleration * Mathf.Max(0f, deltaTime);

            return Mathf.Max(
                nextVerticalVelocity,
                -settings.MaximumFallSpeed);
        }
    }
}
