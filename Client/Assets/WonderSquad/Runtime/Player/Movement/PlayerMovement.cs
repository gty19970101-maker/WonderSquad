using UnityEngine;
using WonderSquad.Core.Logging;
using WonderSquad.Player.Input;

namespace WonderSquad.Player.Movement
{
    /// <summary>
    /// Converts device-independent move input into CharacterController motion.
    /// It does not own input bindings, camera behavior, or network authority.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(GroundDetector))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Read-only local movement input source.")]
        private PlayerInputReader inputReader;

        [SerializeField]
        [Tooltip("CharacterController that applies the calculated displacement.")]
        private CharacterController characterController;

        [SerializeField]
        [Tooltip("Ground state source updated from CharacterController collisions.")]
        private GroundDetector groundDetector;

        [SerializeField]
        [Tooltip("Data asset containing all baseline movement parameters.")]
        private MovementSettings movementSettings;

        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public Vector3 HorizontalVelocity => horizontalVelocity;

        public float VerticalVelocity => verticalVelocity;

        public bool IsGrounded =>
            groundDetector != null &&
            groundDetector.IsGrounded;

        public bool HasValidConfiguration =>
            inputReader != null &&
            characterController != null &&
            groundDetector != null &&
            movementSettings != null &&
            movementSettings.IsValid;

        private void Awake()
        {
            ResolveLocalReferences();
        }

        private void OnEnable()
        {
            ResolveLocalReferences();
            if (!HasValidConfiguration)
            {
                ProjectLog.Error(
                    "PlayerMovement requires valid input, controller, ground detector, and settings references.");
                enabled = false;
                return;
            }

            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            groundDetector.Refresh(CollisionFlags.None);
        }

        private void OnDisable()
        {
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void Update()
        {
            ApplyMovement(inputReader.MoveInput, Time.deltaTime);
        }

        private void ApplyMovement(Vector2 moveInput, float deltaTime)
        {
            horizontalVelocity =
                PlayerMovementMath.CalculateHorizontalVelocity(
                    moveInput,
                    movementSettings.WalkingSpeed);
            verticalVelocity =
                PlayerMovementMath.CalculateVerticalVelocity(
                    verticalVelocity,
                    groundDetector.IsGrounded,
                    deltaTime,
                    movementSettings);

            RotateTowards(horizontalVelocity, deltaTime);

            var velocity =
                horizontalVelocity +
                Vector3.up * verticalVelocity;
            var collisionFlags =
                characterController.Move(velocity * deltaTime);
            groundDetector.Refresh(collisionFlags);
        }

        private void RotateTowards(
            Vector3 movementDirection,
            float deltaTime)
        {
            if (movementDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            var targetRotation =
                Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                movementSettings.RotationSpeedDegreesPerSecond * deltaTime);
        }

        private void ResolveLocalReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (groundDetector == null)
            {
                groundDetector = GetComponent<GroundDetector>();
            }
        }
    }
}
