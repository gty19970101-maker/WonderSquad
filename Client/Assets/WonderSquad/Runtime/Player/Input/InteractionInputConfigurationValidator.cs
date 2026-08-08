using UnityEngine.InputSystem;
using WonderSquad.Core.Input;

namespace WonderSquad.Player.Input
{
    /// <summary>
    /// Validates the approved primary-interaction action without enabling it.
    /// </summary>
    public static class InteractionInputConfigurationValidator
    {
        public static bool TryGetInteractAction(
            InputActionAsset inputActions,
            out InputAction interactAction,
            out string error)
        {
            interactAction = null;
            if (inputActions == null)
            {
                error = "Input Actions asset is missing.";
                return false;
            }

            var gameplayMap = inputActions.FindActionMap(
                InputActionNames.GameplayMap,
                false);
            if (gameplayMap == null)
            {
                error = "Gameplay action map is missing.";
                return false;
            }

            interactAction = gameplayMap.FindAction(
                InputActionNames.Interact,
                false);
            if (interactAction == null)
            {
                error = "Gameplay/Interact action is missing.";
                return false;
            }

            if (interactAction.type != InputActionType.Button)
            {
                error = "Gameplay/Interact must be a Button action.";
                interactAction = null;
                return false;
            }

            if (interactAction.bindings.Count == 0)
            {
                error = "Gameplay/Interact requires at least one binding.";
                interactAction = null;
                return false;
            }

            error = string.Empty;
            return true;
        }
    }
}
