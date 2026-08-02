using System;
using UnityEngine.InputSystem;
using WonderSquad.Core.Input;
using WonderSquad.Interaction.Prompt;

namespace WonderSquad.UI.Interaction
{
    /// <summary>
    /// Resolves and caches display-only bindings without enabling or consuming
    /// the configured interaction action.
    /// </summary>
    public sealed class InteractionBindingDisplayProvider
    {
        public const string KeyboardMouseBindingGroup =
            "Keyboard&Mouse";
        public const string GamepadBindingGroup =
            "Gamepad";
        public const string MissingBindingDisplayText =
            "Unbound";

        private string keyboardMouseDisplayText =
            MissingBindingDisplayText;
        private string gamepadDisplayText =
            MissingBindingDisplayText;

        public string KeyboardMouseDisplayText =>
            keyboardMouseDisplayText;

        public string GamepadDisplayText =>
            gamepadDisplayText;

        public bool HasKeyboardMouseBinding =>
            keyboardMouseDisplayText !=
            MissingBindingDisplayText;

        public bool HasGamepadBinding =>
            gamepadDisplayText !=
            MissingBindingDisplayText;

        public bool Initialize(InputActionAsset inputActions)
        {
            keyboardMouseDisplayText =
                MissingBindingDisplayText;
            gamepadDisplayText =
                MissingBindingDisplayText;

            var gameplayMap = inputActions?.FindActionMap(
                InputActionNames.GameplayMap,
                false);
            var interactAction = gameplayMap?.FindAction(
                InputActionNames.Interact,
                false);
            if (interactAction == null)
            {
                return false;
            }

            for (var index = 0;
                 index < interactAction.bindings.Count;
                 index++)
            {
                var binding = interactAction.bindings[index];
                if (binding.isComposite ||
                    binding.isPartOfComposite)
                {
                    continue;
                }

                if (MatchesGroup(
                        binding.groups,
                        KeyboardMouseBindingGroup))
                {
                    keyboardMouseDisplayText =
                        ResolveDisplayText(
                            interactAction,
                            index);
                }

                if (MatchesGroup(
                        binding.groups,
                        GamepadBindingGroup))
                {
                    gamepadDisplayText =
                        ResolveDisplayText(
                            interactAction,
                            index);
                }
            }

            return
                HasKeyboardMouseBinding ||
                HasGamepadBinding;
        }

        public string GetDisplayText(
            InteractionPromptDeviceKind deviceKind)
        {
            return deviceKind ==
                InteractionPromptDeviceKind.Gamepad
                    ? gamepadDisplayText
                    : keyboardMouseDisplayText;
        }

        private static string ResolveDisplayText(
            InputAction action,
            int bindingIndex)
        {
            var displayText = action.GetBindingDisplayString(
                bindingIndex,
                InputBinding.DisplayStringOptions
                    .DontIncludeInteractions);
            return string.IsNullOrWhiteSpace(displayText)
                ? MissingBindingDisplayText
                : displayText;
        }

        private static bool MatchesGroup(
            string bindingGroups,
            string expectedGroup)
        {
            return
                !string.IsNullOrEmpty(bindingGroups) &&
                bindingGroups.IndexOf(
                    expectedGroup,
                    StringComparison.Ordinal) >= 0;
        }
    }
}
