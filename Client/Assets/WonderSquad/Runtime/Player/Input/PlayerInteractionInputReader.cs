using System;
using UnityEngine;
using UnityEngine.InputSystem;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Logging;

namespace WonderSquad.Player.Input
{
    /// <summary>
    /// Reads primary-interaction press and release edges without executing an
    /// interaction or changing player movement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionInputReader :
        MonoBehaviour,
        IInteractionInputSource
    {
        [SerializeField]
        [Tooltip("Approved WonderSquad Input Actions asset containing Gameplay/Interact.")]
        private InputActionAsset inputActions;

        private InputAction runtimeInteractAction;
        private bool isInteractionHeld;

        public event Action InteractionPressed;

        public event Action InteractionReleased;

        public bool IsInputAvailable =>
            isActiveAndEnabled &&
            runtimeInteractAction != null &&
            runtimeInteractAction.enabled;

        public bool IsInteractionHeld => isInteractionHeld;

        public bool HasValidConfiguration =>
            InteractionInputConfigurationValidator.TryGetInteractAction(
                inputActions,
                out _,
                out _);

        /// <summary>
        /// Supplies the approved Input Actions asset before this component is
        /// enabled.
        /// </summary>
        public bool Configure(InputActionAsset actionAsset)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "PlayerInteractionInputReader cannot be reconfigured while enabled.");
                return false;
            }

            ReleaseRuntimeAction();
            inputActions = actionAsset;
            return
                InteractionInputConfigurationValidator.TryGetInteractAction(
                    inputActions,
                    out _,
                    out _);
        }

        private void OnEnable()
        {
            if (!TryCreateRuntimeAction(out var error))
            {
                ProjectLog.Error(
                    $"PlayerInteractionInputReader is disabled: {error}");
                enabled = false;
                return;
            }

            runtimeInteractAction.performed += OnInteractionPerformed;
            runtimeInteractAction.canceled += OnInteractionCanceled;
            runtimeInteractAction.Enable();
        }

        private void OnDisable()
        {
            if (runtimeInteractAction != null)
            {
                runtimeInteractAction.performed -= OnInteractionPerformed;
                runtimeInteractAction.canceled -= OnInteractionCanceled;
                runtimeInteractAction.Disable();
            }

            isInteractionHeld = false;
            ReleaseRuntimeAction();
        }

        private void OnDestroy()
        {
            ReleaseRuntimeAction();
        }

        private bool TryCreateRuntimeAction(out string error)
        {
            if (runtimeInteractAction != null)
            {
                error = string.Empty;
                return true;
            }

            if (!InteractionInputConfigurationValidator.TryGetInteractAction(
                    inputActions,
                    out var configuredAction,
                    out error))
            {
                return false;
            }

            runtimeInteractAction = new InputAction(
                configuredAction.name,
                InputActionType.Button,
                interactions: configuredAction.interactions,
                processors: configuredAction.processors,
                expectedControlType: configuredAction.expectedControlType);

            foreach (var binding in configuredAction.bindings)
            {
                runtimeInteractAction.AddBinding(binding);
            }

            return true;
        }

        private void OnInteractionPerformed(
            InputAction.CallbackContext context)
        {
            if (isInteractionHeld)
            {
                return;
            }

            isInteractionHeld = true;
            InteractionPressed?.Invoke();
        }

        private void OnInteractionCanceled(
            InputAction.CallbackContext context)
        {
            if (!isInteractionHeld)
            {
                return;
            }

            isInteractionHeld = false;
            InteractionReleased?.Invoke();
        }

        private void ReleaseRuntimeAction()
        {
            if (runtimeInteractAction == null)
            {
                return;
            }

            runtimeInteractAction.Dispose();
            runtimeInteractAction = null;
        }
    }
}
