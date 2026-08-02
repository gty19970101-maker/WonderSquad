using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Logging;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Prompt;
using WonderSquad.Player.Spawning;

namespace WonderSquad.UI.Interaction
{
    /// <summary>
    /// Converts the local detector target into immutable prompt data and
    /// forwards it to a display-only view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptPresenter : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit local Sandbox player source.")]
        private PlayerSpawner playerSpawner;

        [SerializeField]
        [Tooltip("Display-only prompt view.")]
        private InteractionPromptView promptView;

        [SerializeField]
        [Tooltip("Approved WonderSquad Input Actions asset used for binding labels.")]
        private InputActionAsset inputActions;

        [SerializeField]
        [Tooltip("Device label shown until a keyboard, mouse, or gamepad event is observed.")]
        private InteractionPromptDeviceKind initialDeviceKind =
            InteractionPromptDeviceKind.KeyboardMouse;

        private readonly InteractionBindingDisplayProvider
            bindingDisplayProvider =
                new InteractionBindingDisplayProvider();

        private GameObject boundPlayer;
        private InteractionDetector boundDetector;
        private InteractionPromptDeviceKind currentDeviceKind;
        private InteractionTargetId currentTargetId;
        private string currentPromptId = string.Empty;
        private string currentActionTextKey = string.Empty;
        private string currentActionText = string.Empty;
        private bool hasCurrentPromptContent;
        private bool isSpawnerSubscribed;
        private bool isInputEventSubscribed;
        private bool isViewSubscribed;

        public InteractionPromptData CurrentData =>
            promptView == null
                ? default
                : promptView.DisplayedData;

        public InteractionDetector BoundDetector =>
            boundDetector;

        public InteractionPromptDeviceKind CurrentDeviceKind =>
            currentDeviceKind;

        public bool IsBound =>
            boundPlayer != null &&
            boundDetector != null;

        public bool HasValidConfiguration =>
            playerSpawner != null &&
            promptView != null &&
            promptView.HasValidConfiguration;

        private void OnEnable()
        {
            currentDeviceKind =
                IsSupportedDeviceKind(initialDeviceKind)
                    ? initialDeviceKind
                    : InteractionPromptDeviceKind.KeyboardMouse;
            bindingDisplayProvider.Initialize(inputActions);
            ClearPromptContent();

            if (!HasValidConfiguration)
            {
                ProjectLog.Error(
                    "InteractionPromptPresenter requires an explicit " +
                    "PlayerSpawner and configured InteractionPromptView.");
                enabled = false;
                return;
            }

            SubscribeToView();
            SubscribeToSpawner();
            SubscribeToInputEvents();
            HidePrompt();
            BindPlayer(playerSpawner.SpawnedPlayer);
        }

        private void OnDisable()
        {
            UnsubscribeFromInputEvents();
            UnsubscribeFromSpawner();
            UnsubscribeFromView();
            UnbindDetector();
            boundPlayer = null;
            ClearPromptContent();

            if (promptView != null)
            {
                promptView.ResetPresentation();
            }
        }

        /// <summary>
        /// Supplies explicit scene composition references before enable.
        /// </summary>
        public bool Configure(
            PlayerSpawner localPlayerSpawner,
            InteractionPromptView localPromptView,
            InputActionAsset approvedInputActions)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "InteractionPromptPresenter cannot be reconfigured " +
                    "while enabled.");
                return false;
            }

            playerSpawner = localPlayerSpawner;
            promptView = localPromptView;
            inputActions = approvedInputActions;
            return
                playerSpawner != null &&
                promptView != null &&
                promptView.HasValidConfiguration;
        }

        private void BindPlayer(GameObject player)
        {
            UnbindDetector();
            boundPlayer = player;
            if (boundPlayer == null)
            {
                HidePrompt();
                return;
            }

            var detector =
                boundPlayer.GetComponent<InteractionDetector>();
            if (detector == null)
            {
                HidePrompt();
                return;
            }

            boundDetector = detector;
            boundDetector.CurrentTargetChanged +=
                HandleCurrentTargetChanged;
            HandleCurrentTargetChanged(
                boundDetector.CurrentTarget);
        }

        private void UnbindDetector()
        {
            if (boundDetector != null)
            {
                boundDetector.CurrentTargetChanged -=
                    HandleCurrentTargetChanged;
            }

            boundDetector = null;
            HidePrompt();
        }

        private void HandlePlayerSpawned(GameObject player)
        {
            BindPlayer(player);
        }

        private void HandleViewBecameAvailable()
        {
            if (hasCurrentPromptContent)
            {
                ShowCurrentPrompt();
            }
            else
            {
                HidePrompt();
            }
        }

        private void HandleCurrentTargetChanged(
            IInteractable target)
        {
            ClearPromptContent();
            if (!IsAlive(target))
            {
                HidePrompt();
                return;
            }

            if (!(target is Component targetComponent))
            {
                HidePrompt();
                return;
            }

            var promptSource =
                targetComponent
                    .GetComponentInParent<IInteractionPromptSource>();
            if (promptSource == null ||
                !promptSource.HasValidPromptConfiguration)
            {
                HidePrompt();
                return;
            }

            var definition = promptSource.Definition;
            currentTargetId = target.TargetId;
            currentPromptId = definition.PromptId;
            currentActionTextKey =
                definition.ActionTextKey;
            currentActionText =
                definition.FallbackActionText;
            hasCurrentPromptContent = true;
            ShowCurrentPrompt();
        }

        private void HandleInputEvent(
            InputEventPtr inputEvent,
            InputDevice device)
        {
            if (!inputEvent.valid || device == null)
            {
                return;
            }

            var nextDeviceKind =
                ResolveDeviceKind(device);
            if (!IsSupportedDeviceKind(nextDeviceKind) ||
                nextDeviceKind == currentDeviceKind)
            {
                return;
            }

            currentDeviceKind = nextDeviceKind;
            if (hasCurrentPromptContent)
            {
                ShowCurrentPrompt();
            }
            else
            {
                HidePrompt();
            }
        }

        private void ShowCurrentPrompt()
        {
            if (!hasCurrentPromptContent ||
                promptView == null)
            {
                HidePrompt();
                return;
            }

            var promptData = new InteractionPromptData(
                currentTargetId,
                currentPromptId,
                currentActionTextKey,
                currentActionText,
                currentDeviceKind,
                bindingDisplayProvider.GetDisplayText(
                    currentDeviceKind),
                true);
            promptView.Render(promptData);
        }

        private void HidePrompt()
        {
            if (promptView == null)
            {
                return;
            }

            promptView.Render(
                InteractionPromptData.CreateHidden(
                    currentDeviceKind,
                    bindingDisplayProvider.GetDisplayText(
                        currentDeviceKind)));
        }

        private void ClearPromptContent()
        {
            currentTargetId = default;
            currentPromptId = string.Empty;
            currentActionTextKey = string.Empty;
            currentActionText = string.Empty;
            hasCurrentPromptContent = false;
        }

        private void SubscribeToSpawner()
        {
            if (isSpawnerSubscribed ||
                playerSpawner == null)
            {
                return;
            }

            playerSpawner.PlayerSpawned +=
                HandlePlayerSpawned;
            isSpawnerSubscribed = true;
        }

        private void SubscribeToView()
        {
            if (isViewSubscribed ||
                promptView == null)
            {
                return;
            }

            promptView.BecameAvailable +=
                HandleViewBecameAvailable;
            isViewSubscribed = true;
        }

        private void UnsubscribeFromView()
        {
            if (!isViewSubscribed)
            {
                return;
            }

            if (promptView != null)
            {
                promptView.BecameAvailable -=
                    HandleViewBecameAvailable;
            }

            isViewSubscribed = false;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSpawnerSubscribed)
            {
                return;
            }

            if (playerSpawner != null)
            {
                playerSpawner.PlayerSpawned -=
                    HandlePlayerSpawned;
            }

            isSpawnerSubscribed = false;
        }

        private void SubscribeToInputEvents()
        {
            if (isInputEventSubscribed)
            {
                return;
            }

            InputSystem.onEvent += HandleInputEvent;
            isInputEventSubscribed = true;
        }

        private void UnsubscribeFromInputEvents()
        {
            if (!isInputEventSubscribed)
            {
                return;
            }

            InputSystem.onEvent -= HandleInputEvent;
            isInputEventSubscribed = false;
        }

        private static InteractionPromptDeviceKind
            ResolveDeviceKind(InputDevice device)
        {
            if (device is Gamepad)
            {
                return InteractionPromptDeviceKind.Gamepad;
            }

            if (device is Keyboard || device is Mouse)
            {
                return
                    InteractionPromptDeviceKind.KeyboardMouse;
            }

            return InteractionPromptDeviceKind.Unknown;
        }

        private static bool IsSupportedDeviceKind(
            InteractionPromptDeviceKind deviceKind)
        {
            return
                deviceKind ==
                    InteractionPromptDeviceKind.KeyboardMouse ||
                deviceKind ==
                    InteractionPromptDeviceKind.Gamepad;
        }

        private static bool IsAlive(IInteractable target)
        {
            if (target == null)
            {
                return false;
            }

            return
                !(target is UnityEngine.Object unityObject) ||
                unityObject != null;
        }
    }
}
