using System;
using UnityEngine;
using UnityEngine.InputSystem;
using WonderSquad.Core.Logging;

namespace WonderSquad.Player.Input
{
    /// <summary>
    /// Reads a local move action without applying movement or owning a device.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Vector2 Move action from the approved WonderSquad Input Actions asset.")]
        private InputActionReference moveActionReference;

        private InputAction runtimeMoveAction;
        private Vector2 moveInput;

        public event Action<Vector2> MoveInputChanged;

        public Vector2 MoveInput => moveInput;

        public bool HasValidConfiguration =>
            PlayerInputConfigurationValidator.TryValidateMoveAction(
                moveActionReference,
                out _);

        /// <summary>
        /// Supplies an action reference before this component is enabled.
        /// Intended for explicit runtime composition and isolated tests.
        /// </summary>
        public bool Configure(InputActionReference actionReference)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "PlayerInputReader cannot be reconfigured while enabled.");
                return false;
            }

            ReleaseRuntimeAction();
            moveActionReference = actionReference;
            return PlayerInputConfigurationValidator.TryValidateMoveAction(
                moveActionReference,
                out _);
        }

        private void OnEnable()
        {
            if (!TryCreateRuntimeAction(out var error))
            {
                ProjectLog.Error($"PlayerInputReader is disabled: {error}");
                enabled = false;
                return;
            }

            runtimeMoveAction.performed += OnMoveActionChanged;
            runtimeMoveAction.canceled += OnMoveActionChanged;
            runtimeMoveAction.Enable();
            SetMoveInput(runtimeMoveAction.ReadValue<Vector2>());
        }

        private void OnDisable()
        {
            if (runtimeMoveAction != null)
            {
                runtimeMoveAction.performed -= OnMoveActionChanged;
                runtimeMoveAction.canceled -= OnMoveActionChanged;
                runtimeMoveAction.Disable();
            }

            ReleaseRuntimeAction();
            SetMoveInput(Vector2.zero);
        }

        private void OnDestroy()
        {
            ReleaseRuntimeAction();
        }

        private bool TryCreateRuntimeAction(out string error)
        {
            if (runtimeMoveAction != null)
            {
                error = string.Empty;
                return true;
            }

            if (!PlayerInputConfigurationValidator.TryValidateMoveAction(
                    moveActionReference,
                    out error))
            {
                return false;
            }

            var configuredAction = moveActionReference.action;
            runtimeMoveAction = new InputAction(
                configuredAction.name,
                InputActionType.PassThrough,
                interactions: configuredAction.interactions,
                processors: configuredAction.processors,
                expectedControlType: configuredAction.expectedControlType);

            foreach (var binding in configuredAction.bindings)
            {
                runtimeMoveAction.AddBinding(binding);
            }

            return true;
        }

        private void OnMoveActionChanged(InputAction.CallbackContext context)
        {
            SetMoveInput(context.ReadValue<Vector2>());
        }

        private void SetMoveInput(Vector2 rawInput)
        {
            var nextMoveInput = MoveInputValue.Clamp(rawInput);
            if (moveInput == nextMoveInput)
            {
                return;
            }

            moveInput = nextMoveInput;
            MoveInputChanged?.Invoke(moveInput);
        }

        private void ReleaseRuntimeAction()
        {
            if (runtimeMoveAction == null)
            {
                return;
            }

            runtimeMoveAction.Dispose();
            runtimeMoveAction = null;
        }
    }
}
