using UnityEngine;

namespace WonderSquad.Player.Movement
{
    /// <summary>
    /// Stores the tunable parameters for baseline CharacterController movement.
    /// Runtime movement state must not be written back to this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "MovementSettings",
        menuName = "Wonder Squad/Player/Movement Settings")]
    public sealed class MovementSettings : ScriptableObject
    {
        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum horizontal walking speed in world units per second.")]
        private float walkingSpeed = 4f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum turning speed in degrees per second.")]
        private float rotationSpeedDegreesPerSecond = 540f;

        [SerializeField]
        [Tooltip("Downward acceleration in world units per second squared.")]
        private float gravityAcceleration = -25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum downward speed in world units per second.")]
        private float maximumFallSpeed = 30f;

        [SerializeField]
        [Tooltip("Small downward speed used to keep the controller grounded.")]
        private float groundedVerticalSpeed = -2f;

        public float WalkingSpeed => walkingSpeed;

        public float RotationSpeedDegreesPerSecond =>
            rotationSpeedDegreesPerSecond;

        public float GravityAcceleration => gravityAcceleration;

        public float MaximumFallSpeed => maximumFallSpeed;

        public float GroundedVerticalSpeed => groundedVerticalSpeed;

        public bool IsValid =>
            IsFinite(walkingSpeed) &&
            walkingSpeed > 0f &&
            IsFinite(rotationSpeedDegreesPerSecond) &&
            rotationSpeedDegreesPerSecond > 0f &&
            IsFinite(gravityAcceleration) &&
            gravityAcceleration < 0f &&
            IsFinite(maximumFallSpeed) &&
            maximumFallSpeed > 0f &&
            IsFinite(groundedVerticalSpeed) &&
            groundedVerticalSpeed <= 0f &&
            groundedVerticalSpeed >= -maximumFallSpeed;

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
