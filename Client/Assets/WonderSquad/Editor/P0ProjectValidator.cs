using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Input;

namespace WonderSquad.Editor
{
    public static class P0ProjectValidator
    {
        [MenuItem("Wonder Squad/P0/Validate Project Skeleton")]
        public static void ValidateProjectSkeleton()
        {
            var errors = CollectValidationErrors();

            if (errors.Count == 0)
            {
                Debug.Log("[WonderSquad] P0 project skeleton validation passed.");
                return;
            }

            foreach (var error in errors)
            {
                Debug.LogError($"[WonderSquad] {error}");
            }
        }

        public static IReadOnlyList<string> CollectValidationErrors()
        {
            var errors = new List<string>();
            var requiredScenes = new[]
            {
                ProjectConstants.BootstrapScenePath,
                ProjectConstants.PlayerSandboxScenePath,
                ProjectConstants.GameplaySandboxScenePath,
                ProjectConstants.RecoverySandboxScenePath
            };

            foreach (var scenePath in requiredScenes)
            {
                RequireAsset(scenePath, errors);
            }

            var enabledScenes = new HashSet<string>();
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    enabledScenes.Add(scene.path);
                }
            }

            foreach (var scenePath in requiredScenes)
            {
                if (!enabledScenes.Contains(scenePath))
                {
                    errors.Add($"Required scene is not enabled in Build Settings: {scenePath}");
                }
            }

            if (GraphicsSettings.defaultRenderPipeline == null)
            {
                errors.Add("Graphics Settings has no default render pipeline.");
            }

            if (QualitySettings.renderPipeline == null)
            {
                errors.Add("The active Quality level has no render pipeline.");
            }

            ValidateInputActions(errors);
            return errors;
        }

        private static void ValidateInputActions(ICollection<string> errors)
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                ProjectConstants.InputActionsAssetPath);
            if (inputActions == null)
            {
                errors.Add(
                    $"Required Input Actions asset is missing or failed to import: " +
                    ProjectConstants.InputActionsAssetPath);
                return;
            }

            var gameplayMap = inputActions.FindActionMap(
                InputActionNames.GameplayMap,
                false);
            if (gameplayMap == null)
            {
                errors.Add($"Input action map is missing: {InputActionNames.GameplayMap}");
                return;
            }

            var requiredActions = new[]
            {
                InputActionNames.Move,
                InputActionNames.Look,
                InputActionNames.Jump,
                InputActionNames.Interact,
                InputActionNames.Ability,
                InputActionNames.Marker,
                InputActionNames.QuickIntent,
                InputActionNames.Emote,
                InputActionNames.Rescue,
                InputActionNames.Pause
            };

            foreach (var actionName in requiredActions)
            {
                var action = gameplayMap.FindAction(actionName, false);
                if (action == null)
                {
                    errors.Add($"Input action is missing: {actionName}");
                    continue;
                }

                var hasKeyboardAndMouseBinding = false;
                var hasGamepadBinding = false;
                foreach (var binding in action.bindings)
                {
                    var groups = binding.groups ?? string.Empty;
                    hasKeyboardAndMouseBinding |=
                        groups.Contains("Keyboard&Mouse");
                    hasGamepadBinding |= groups.Contains("Gamepad");
                }

                if (!hasKeyboardAndMouseBinding)
                {
                    errors.Add(
                        $"Input action has no Keyboard&Mouse binding: {actionName}");
                }

                if (!hasGamepadBinding)
                {
                    errors.Add($"Input action has no Gamepad binding: {actionName}");
                }
            }

            var hasKeyboardAndMouseScheme = false;
            var hasGamepadScheme = false;
            foreach (var controlScheme in inputActions.controlSchemes)
            {
                hasKeyboardAndMouseScheme |=
                    controlScheme.name == "Keyboard&Mouse";
                hasGamepadScheme |= controlScheme.name == "Gamepad";
            }

            if (!hasKeyboardAndMouseScheme)
            {
                errors.Add("Input control scheme is missing: Keyboard&Mouse");
            }

            if (!hasGamepadScheme)
            {
                errors.Add("Input control scheme is missing: Gamepad");
            }
        }

        private static void RequireAsset(string assetPath, ICollection<string> errors)
        {
            if (AssetDatabase.LoadMainAssetAtPath(assetPath) == null)
            {
                errors.Add($"Required asset is missing: {assetPath}");
            }
        }
    }
}
