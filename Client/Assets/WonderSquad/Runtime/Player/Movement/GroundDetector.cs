using UnityEngine;
using WonderSquad.Core.Logging;

namespace WonderSquad.Player.Movement
{
    /// <summary>
    /// Exposes the CharacterController ground result without owning movement.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class GroundDetector : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("CharacterController whose collision result defines grounding.")]
        private CharacterController characterController;

        public bool IsGrounded { get; private set; }

        public bool HasValidConfiguration => characterController != null;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void OnEnable()
        {
            if (!HasValidConfiguration)
            {
                ProjectLog.Error(
                    "GroundDetector requires a CharacterController.");
                enabled = false;
                return;
            }

            Refresh(CollisionFlags.None);
        }

        private void OnDisable()
        {
            IsGrounded = false;
        }

        /// <summary>
        /// Captures the ground result from the latest controller move.
        /// </summary>
        public void Refresh(CollisionFlags collisionFlags)
        {
            IsGrounded =
                characterController != null &&
                characterController.enabled &&
                (characterController.isGrounded ||
                 (collisionFlags & CollisionFlags.Below) != 0);
        }
    }
}
