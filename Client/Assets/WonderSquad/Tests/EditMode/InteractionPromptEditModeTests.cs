using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Interaction.Prompt;
using WonderSquad.Player.Spawning;
using WonderSquad.UI.Interaction;

namespace WonderSquad.Tests.EditMode
{
    public sealed class InteractionPromptEditModeTests
    {
        private const string PromptDefinitionPath =
            "Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_Default.asset";
        private const string PromptPrefabPath =
            "Assets/WonderSquad/Prefabs/UI/PF_InteractionPrompt.prefab";
        private const string CoreAssemblyPath =
            "Assets/WonderSquad/Runtime/Core/WonderSquad.Core.asmdef";
        private const string PlayerAssemblyPath =
            "Assets/WonderSquad/Runtime/Player/WonderSquad.Player.asmdef";
        private const string InteractionAssemblyPath =
            "Assets/WonderSquad/Runtime/Interaction/WonderSquad.Interaction.asmdef";
        private const string UiAssemblyPath =
            "Assets/WonderSquad/Runtime/UI/WonderSquad.UI.asmdef";
        private const string PackageManifestPath =
            "Packages/manifest.json";
        private const string PackageLockPath =
            "Packages/packages-lock.json";

        [Test]
        public void PromptData_ValidContentIsDisplayableAndReadOnly()
        {
            var promptData = CreateVisibleData(
                "interaction.test.prompt_data",
                "interaction.prompt.test",
                "Test Action",
                InteractionPromptDeviceKind.KeyboardMouse,
                "E");

            Assert.That(promptData.HasValidContent, Is.True);
            Assert.That(promptData.IsDisplayable, Is.True);
            Assert.That(
                typeof(InteractionPromptData).IsValueType,
                Is.True);
            FieldInfo[] fields =
                typeof(InteractionPromptData).GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            Assert.That(fields, Is.Not.Empty);

            for (var fieldIndex = 0;
                 fieldIndex < fields.Length;
                 fieldIndex++)
            {
                Assert.That(
                    fields[fieldIndex].IsInitOnly,
                    Is.True,
                    $"Prompt data field '{fields[fieldIndex].Name}' must remain read-only.");
            }
        }

        [Test]
        public void PromptData_EmptyTargetProducesSafeHiddenState()
        {
            var hiddenData =
                InteractionPromptData.CreateHidden(
                    InteractionPromptDeviceKind.KeyboardMouse,
                    InteractionBindingDisplayProvider
                        .MissingBindingDisplayText);

            Assert.That(hiddenData.TargetId.IsValid, Is.False);
            Assert.That(hiddenData.IsVisible, Is.False);
            Assert.That(hiddenData.IsDisplayable, Is.False);
            Assert.That(hiddenData.PromptId, Is.Empty);
            Assert.That(hiddenData.ActionText, Is.Empty);
        }

        [Test]
        public void PromptDefinitionAndPrefab_AssetsAreValid()
        {
            var definition =
                AssetDatabase
                    .LoadAssetAtPath<InteractionPromptDefinition>(
                        PromptDefinitionPath);
            var promptPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PromptPrefabPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.IsValid, Is.True);
            Assert.That(promptPrefab, Is.Not.Null);
            Assert.That(
                promptPrefab.GetComponent<InteractionPromptPresenter>(),
                Is.Not.Null);

            var view =
                promptPrefab.GetComponent<InteractionPromptView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasValidConfiguration, Is.True);
            Assert.That(view.IsVisible, Is.False);
        }

        [Test]
        public void PromptDefinition_InvalidContentIsRejected()
        {
            var definition =
                ScriptableObject
                    .CreateInstance<InteractionPromptDefinition>();

            try
            {
                Assert.That(
                    definition.Configure(
                        string.Empty,
                        "interaction.prompt.test",
                        "Test"),
                    Is.False);
                Assert.That(
                    definition.Configure(
                        "interaction.prompt.test",
                        string.Empty,
                        "Test"),
                    Is.False);
                Assert.That(
                    definition.Configure(
                        "interaction.prompt.test",
                        "interaction.prompt.test",
                        string.Empty),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void BindingDisplayProvider_MissingAssetUsesSafeFallback()
        {
            var provider =
                new InteractionBindingDisplayProvider();

            Assert.That(provider.Initialize(null), Is.False);
            Assert.That(
                provider.GetDisplayText(
                    InteractionPromptDeviceKind.KeyboardMouse),
                Is.EqualTo(
                    InteractionBindingDisplayProvider
                        .MissingBindingDisplayText));
            Assert.That(
                provider.GetDisplayText(
                    InteractionPromptDeviceKind.Gamepad),
                Is.EqualTo(
                    InteractionBindingDisplayProvider
                        .MissingBindingDisplayText));
        }

        [Test]
        public void BindingDisplayProvider_ResolvesBindingsWithoutEnablingAction()
        {
            var inputActions = CreateInputActions();
            var provider =
                new InteractionBindingDisplayProvider();
            var interactAction =
                inputActions.FindAction("Interact", true);

            try
            {
                Assert.That(interactAction.enabled, Is.False);
                Assert.That(
                    provider.Initialize(inputActions),
                    Is.True);
                Assert.That(interactAction.enabled, Is.False);
                Assert.That(
                    provider.KeyboardMouseDisplayText,
                    Is.Not.EqualTo(
                        InteractionBindingDisplayProvider
                            .MissingBindingDisplayText));
                Assert.That(
                    provider.GamepadDisplayText,
                    Is.Not.EqualTo(
                        InteractionBindingDisplayProvider
                            .MissingBindingDisplayText));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(inputActions);
            }
        }

        [Test]
        public void PromptView_SameDataDoesNotRefreshAndTargetSwitchDoes()
        {
            var viewRoot =
                new GameObject("InteractionPromptViewTest");
            viewRoot.SetActive(false);
            var view = CreateView(viewRoot);
            var firstData = CreateVisibleData(
                "interaction.test.first",
                "interaction.prompt.first",
                "First",
                InteractionPromptDeviceKind.KeyboardMouse,
                "E");
            var secondData = CreateVisibleData(
                "interaction.test.second",
                "interaction.prompt.second",
                "Second",
                InteractionPromptDeviceKind.KeyboardMouse,
                "E");

            try
            {
                Assert.That(view.Render(firstData), Is.True);
                var refreshCount = view.RefreshCount;

                Assert.That(view.Render(firstData), Is.False);
                Assert.That(view.RefreshCount, Is.EqualTo(refreshCount));

                Assert.That(view.Render(secondData), Is.True);
                Assert.That(
                    view.RefreshCount,
                    Is.EqualTo(refreshCount + 1));
                Assert.That(
                    view.DisplayedData.TargetId,
                    Is.EqualTo(secondData.TargetId));
                Assert.That(view.DisplayedActionText, Is.EqualTo("Second"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(viewRoot);
            }
        }

        [Test]
        public void Presenter_EnableDisableTransitionsRemainHiddenWithoutTarget()
        {
            var presenterRoot =
                new GameObject("InteractionPromptPresenterTest");
            presenterRoot.SetActive(false);
            var view = CreateView(presenterRoot);
            var presenter =
                presenterRoot
                    .AddComponent<InteractionPromptPresenter>();
            var spawnerRoot =
                new GameObject("InteractionPromptSpawnerTest");
            var spawner =
                spawnerRoot.AddComponent<PlayerSpawner>();
            var inputActions = CreateInputActions();

            try
            {
                Assert.That(
                    presenter.Configure(
                        spawner,
                        view,
                        inputActions),
                    Is.True);

                presenterRoot.SetActive(true);
                Assert.That(presenter.IsBound, Is.False);
                Assert.That(view.IsVisible, Is.False);

                presenter.enabled = false;
                Assert.That(view.IsVisible, Is.False);
                Assert.That(view.DisplayedActionText, Is.Empty);

                presenter.enabled = true;
                Assert.That(view.IsVisible, Is.False);
                Assert.That(presenter.CurrentData.IsVisible, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterRoot);
                UnityEngine.Object.DestroyImmediate(spawnerRoot);
                UnityEngine.Object.DestroyImmediate(inputActions);
            }
        }

        [Test]
        public void PromptView_DoesNotReferenceInteractableContract()
        {
            var viewType = typeof(InteractionPromptView);
            var fields = viewType.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                Assert.That(
                    field.FieldType,
                    Is.Not.EqualTo(typeof(IInteractable)));
            }
        }

        [Test]
        public void RuntimeAssemblyDependencies_RemainAcyclic()
        {
            var coreAssembly =
                LoadAssemblyDefinition(CoreAssemblyPath);
            var playerAssembly =
                LoadAssemblyDefinition(PlayerAssemblyPath);
            var interactionAssembly =
                LoadAssemblyDefinition(InteractionAssemblyPath);
            var uiAssembly =
                LoadAssemblyDefinition(UiAssemblyPath);

            Assert.That(coreAssembly.references, Is.Empty);
            Assert.That(
                playerAssembly.references,
                Does.Not.Contain("WonderSquad.Interaction"));
            Assert.That(
                playerAssembly.references,
                Does.Not.Contain("WonderSquad.UI"));
            Assert.That(
                interactionAssembly.references,
                Is.EqualTo(new[] { "WonderSquad.Core" }));
            Assert.That(
                uiAssembly.references,
                Does.Contain("WonderSquad.Interaction"));
            Assert.That(
                uiAssembly.references,
                Does.Contain("WonderSquad.Player"));
            Assert.That(
                uiAssembly.references,
                Does.Contain("UnityEngine.UI"));
        }

        [Test]
        public void UguiPackage_IsDirectlyLockedToExactVersion()
        {
            var manifest = File.ReadAllText(PackageManifestPath);
            var packageLock = File.ReadAllText(PackageLockPath);
            const string exactManifestEntry =
                "\"com.unity.ugui\": \"2.0.0\"";
            const string lockPattern =
                "\"com\\.unity\\.ugui\"\\s*:\\s*\\{\\s*" +
                "\"version\"\\s*:\\s*\"2\\.0\\.0\"\\s*,\\s*" +
                "\"depth\"\\s*:\\s*0";

            Assert.That(
                manifest,
                Does.Contain(exactManifestEntry));
            Assert.That(
                Regex.IsMatch(
                    packageLock,
                    lockPattern,
                    RegexOptions.Singleline),
                Is.True);
        }

        private static InteractionPromptView CreateView(
            GameObject viewRoot)
        {
            var contentRoot =
                new GameObject(
                    "PromptContent",
                    typeof(RectTransform));
            contentRoot.transform.SetParent(
                viewRoot.transform,
                false);

            var bindingRoot =
                new GameObject(
                    "BindingText",
                    typeof(RectTransform));
            bindingRoot.transform.SetParent(
                contentRoot.transform,
                false);
            var bindingText =
                bindingRoot.AddComponent<Text>();

            var actionRoot =
                new GameObject(
                    "ActionText",
                    typeof(RectTransform));
            actionRoot.transform.SetParent(
                contentRoot.transform,
                false);
            var actionText =
                actionRoot.AddComponent<Text>();

            var view =
                viewRoot.AddComponent<InteractionPromptView>();
            Assert.That(
                view.Configure(
                    contentRoot,
                    bindingText,
                    actionText),
                Is.True);
            return view;
        }

        private static InputActionAsset CreateInputActions()
        {
            var inputActions =
                ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplayMap =
                inputActions.AddActionMap("Gameplay");
            var interactAction =
                gameplayMap.AddAction(
                    "Interact",
                    InputActionType.Button);
            interactAction
                .AddBinding("<Keyboard>/e")
                .WithGroup(
                    InteractionBindingDisplayProvider
                        .KeyboardMouseBindingGroup);
            interactAction
                .AddBinding("<Gamepad>/buttonWest")
                .WithGroup(
                    InteractionBindingDisplayProvider
                        .GamepadBindingGroup);
            return inputActions;
        }

        private static InteractionPromptData CreateVisibleData(
            string targetId,
            string promptId,
            string actionText,
            InteractionPromptDeviceKind deviceKind,
            string bindingDisplayText)
        {
            return new InteractionPromptData(
                new InteractionTargetId(targetId),
                promptId,
                $"{promptId}.key",
                actionText,
                deviceKind,
                bindingDisplayText,
                true);
        }

        private static AssemblyDefinitionData LoadAssemblyDefinition(
            string path)
        {
            var json = File.ReadAllText(path);
            var definition =
                JsonUtility.FromJson<AssemblyDefinitionData>(json);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.references, Is.Not.Null);
            return definition;
        }

        [Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string[] references;
        }
    }
}
