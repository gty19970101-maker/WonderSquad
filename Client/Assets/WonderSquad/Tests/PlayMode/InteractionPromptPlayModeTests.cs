using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Prompt;
using WonderSquad.Player.Spawning;
using WonderSquad.UI.Interaction;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class InteractionPromptPlayModeTests :
        InputTestFixture
    {
        private const string InteractableLayerName = "Interactable";
        private const int StablePromptWarmupCount = 32;
        private const int StablePromptObservationCount = 128;
        private const float PositionTolerance = 0.001f;

        private readonly List<InteractionPromptDefinition>
            createdDefinitions =
                new List<InteractionPromptDefinition>();

        [TearDown]
        public override void TearDown()
        {
            foreach (var definition in createdDefinitions)
            {
                if (definition != null)
                {
                    Object.DestroyImmediate(definition);
                }
            }

            createdDefinitions.Clear();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator DefaultHiddenAndNearbyTargetShowsPrompt()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();

            Assert.That(context.View.IsVisible, Is.False);
            var target = CreateTarget(
                "interaction.prompt.nearby",
                "interaction.prompt.nearby",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();

            Assert.That(
                context.Detector.RefreshDetection(),
                Is.True);
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.Presenter.CurrentData.TargetId,
                Is.EqualTo(target.TargetId));
            Assert.That(
                context.View.DisplayedActionText,
                Is.EqualTo("Interact"));
        }

        [UnityTest]
        public IEnumerator TargetLeavingRangeHidesPrompt()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var target = CreateTarget(
                "interaction.prompt.leaving",
                "interaction.prompt.leaving",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            Assert.That(context.View.IsVisible, Is.True);

            target.transform.position =
                context.Detector.DetectionOrigin.position +
                Vector3.right *
                (context.Detector.Settings.DetectionRadius + 1f);
            Physics.SyncTransforms();

            Assert.That(
                context.Detector.RefreshDetection(),
                Is.False);
            Assert.That(context.View.IsVisible, Is.False);
            Assert.That(
                context.View.DisplayedActionText,
                Is.Empty);
        }

        [UnityTest]
        public IEnumerator OcclusionHidesPromptAndClearingRestoresIt()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var originPosition =
                context.Detector.DetectionOrigin.position;
            var targetPosition =
                originPosition + Vector3.right * 2f;
            CreateTarget(
                "interaction.prompt.occluded",
                "interaction.prompt.occluded",
                "Inspect",
                targetPosition);
            var wall =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "InteractionPromptOcclusionWall";
            wall.layer = 0;
            wall.transform.position =
                Vector3.Lerp(
                    originPosition,
                    targetPosition,
                    0.5f);
            wall.transform.localScale =
                new Vector3(0.25f, 2f, 1f);
            Physics.SyncTransforms();

            Assert.That(
                context.Detector.RefreshDetection(),
                Is.False);
            Assert.That(context.View.IsVisible, Is.False);

            wall.SetActive(false);
            Physics.SyncTransforms();

            Assert.That(
                context.Detector.RefreshDetection(),
                Is.True);
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.View.DisplayedActionText,
                Is.EqualTo("Inspect"));
        }

        [UnityTest]
        public IEnumerator MultipleTargetsSwitchPromptContentDeterministically()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var originPosition =
                context.Detector.DetectionOrigin.position;
            var firstTarget = CreateTarget(
                "interaction.prompt.target_a",
                "interaction.prompt.target_a",
                "Target A",
                originPosition + Vector3.right,
                1);
            var secondTarget = CreateTarget(
                "interaction.prompt.target_b",
                "interaction.prompt.target_b",
                "Target B",
                originPosition + Vector3.left,
                0);
            Physics.SyncTransforms();

            context.Detector.RefreshDetection();
            Assert.That(
                context.Presenter.CurrentData.TargetId,
                Is.EqualTo(firstTarget.TargetId));
            Assert.That(
                context.View.DisplayedActionText,
                Is.EqualTo("Target A"));

            firstTarget.Configure(
                firstTarget.TargetId.Value,
                firstTarget.transform,
                true,
                -1);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            Assert.That(
                context.Presenter.CurrentData.TargetId,
                Is.EqualTo(secondTarget.TargetId));
            Assert.That(
                context.View.DisplayedActionText,
                Is.EqualTo("Target B"));
        }

        [UnityTest]
        public IEnumerator DestroyedTargetSafelyHidesPrompt()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var target = CreateTarget(
                "interaction.prompt.destroyed",
                "interaction.prompt.destroyed",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            Assert.That(context.View.IsVisible, Is.True);

            Object.Destroy(target.gameObject);
            yield return null;
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            Assert.That(context.View.IsVisible, Is.False);
            Assert.That(
                context.Presenter.CurrentData.IsVisible,
                Is.False);
        }

        [UnityTest]
        public IEnumerator DetectorDisableHidesAndReenableRestoresPrompt()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            CreateTarget(
                "interaction.prompt.detector_lifecycle",
                "interaction.prompt.detector_lifecycle",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            Assert.That(context.View.IsVisible, Is.True);

            context.Detector.enabled = false;
            Assert.That(context.View.IsVisible, Is.False);

            context.Detector.enabled = true;
            context.Detector.RefreshDetection();
            Assert.That(context.View.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator PresenterAndViewDisableClearAndRestoreState()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            CreateTarget(
                "interaction.prompt.view_lifecycle",
                "interaction.prompt.view_lifecycle",
                "Inspect",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            Assert.That(context.View.IsVisible, Is.True);

            context.Presenter.enabled = false;
            Assert.That(context.View.IsVisible, Is.False);

            context.Presenter.enabled = true;
            Assert.That(context.View.IsVisible, Is.True);

            context.View.enabled = false;
            Assert.That(context.View.IsVisible, Is.False);

            context.View.enabled = true;
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.View.DisplayedActionText,
                Is.EqualTo("Inspect"));
        }

        [UnityTest]
        public IEnumerator InputDeviceSwitchUpdatesCachedBindingDisplay()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            CreateTarget(
                "interaction.prompt.device",
                "interaction.prompt.device",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            Press(gamepad.buttonWest);
            yield return null;
            Assert.That(
                context.Presenter.CurrentData.DeviceKind,
                Is.EqualTo(InteractionPromptDeviceKind.Gamepad));
            Assert.That(
                context.View.DisplayedBindingText,
                Is.Not.Empty);
            Assert.That(
                context.View.DisplayedBindingText,
                Is.Not.EqualTo(
                    InteractionBindingDisplayProvider
                        .MissingBindingDisplayText));
            Release(gamepad.buttonWest);

            Press(keyboard.eKey);
            yield return null;
            Assert.That(
                context.Presenter.CurrentData.DeviceKind,
                Is.EqualTo(
                    InteractionPromptDeviceKind.KeyboardMouse));
            Assert.That(
                context.View.DisplayedBindingText,
                Does.Contain("E").IgnoreCase);
            Release(keyboard.eKey);
        }

        [UnityTest]
        public IEnumerator PromptSystemDoesNotMovePlayerWithoutInput()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            CreateTarget(
                "interaction.prompt.no_movement",
                "interaction.prompt.no_movement",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            var initialPosition =
                context.Spawner.SpawnedPlayer.transform.position;

            yield return null;
            yield return null;
            yield return null;

            var finalPosition =
                context.Spawner.SpawnedPlayer.transform.position;
            Assert.That(
                Mathf.Abs(finalPosition.x - initialPosition.x),
                Is.LessThanOrEqualTo(PositionTolerance));
            Assert.That(
                Mathf.Abs(finalPosition.z - initialPosition.z),
                Is.LessThanOrEqualTo(PositionTolerance));
            Assert.That(context.View.IsVisible, Is.True);
        }

        [UnityTest]
        public IEnumerator InteractBindingDoesNotChangeTargetState()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var target = CreateTarget(
                "interaction.prompt.read_only",
                "interaction.prompt.read_only",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            var targetIdBefore = target.TargetId;
            var refreshCountBefore =
                context.View.RefreshCount;

            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return null;

            Assert.That(target.TargetId, Is.EqualTo(targetIdBefore));
            Assert.That(target.IsDetectionEnabled, Is.True);
            Assert.That(
                context.Detector.CurrentTarget,
                Is.SameAs(target));
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.View.RefreshCount,
                Is.GreaterThanOrEqualTo(refreshCountBefore));
        }

        [UnityTest]
        public IEnumerator StablePromptRefreshDoesNotAllocateOrRewriteView()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            CreateTarget(
                "interaction.prompt.allocation",
                "interaction.prompt.allocation",
                "Interact",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();

            for (var index = 0;
                 index < StablePromptWarmupCount;
                 index++)
            {
                context.Detector.RefreshDetection();
            }

            var refreshCountBefore =
                context.View.RefreshCount;
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            var allocatedBytesBefore =
                System.GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0;
                 index < StablePromptObservationCount;
                 index++)
            {
                context.Detector.RefreshDetection();
            }

            var allocatedBytesAfter =
                System.GC.GetAllocatedBytesForCurrentThread();

            Assert.That(
                allocatedBytesAfter - allocatedBytesBefore,
                Is.EqualTo(0L));
            Assert.That(
                context.View.RefreshCount,
                Is.EqualTo(refreshCountBefore));
            Assert.That(context.View.IsVisible, Is.True);
        }

        private InteractionTarget CreateTarget(
            string targetId,
            string promptId,
            string actionText,
            Vector3 position,
            int priority = 0)
        {
            var targetObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = targetId;
            targetObject.layer =
                LayerMask.NameToLayer(InteractableLayerName);
            targetObject.transform.position = position;
            targetObject.GetComponent<Collider>().isTrigger = true;

            var target =
                targetObject.AddComponent<InteractionTarget>();
            Assert.That(
                target.Configure(
                    targetId,
                    targetObject.transform,
                    true,
                    priority),
                Is.True);

            var definition =
                ScriptableObject
                    .CreateInstance<InteractionPromptDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            Assert.That(
                definition.Configure(
                    promptId,
                    $"{promptId}.action",
                    actionText),
                Is.True);
            createdDefinitions.Add(definition);

            var source =
                targetObject
                    .AddComponent<InteractionPromptSource>();
            Assert.That(source.Configure(definition), Is.True);
            return target;
        }

        private static IEnumerator LoadPlayerSandbox()
        {
            var loadOperation = SceneManager.LoadSceneAsync(
                ProjectConstants.PlayerSandboxScenePath,
                LoadSceneMode.Single);

            Assert.That(loadOperation, Is.Not.Null);
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static PromptTestContext FindContext()
        {
            var spawners = Object.FindObjectsByType<PlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var presenters =
                Object.FindObjectsByType<InteractionPromptPresenter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            var views =
                Object.FindObjectsByType<InteractionPromptView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(presenters, Has.Length.EqualTo(1));
            Assert.That(views, Has.Length.EqualTo(1));
            Assert.That(spawners[0].SpawnedPlayer, Is.Not.Null);

            var detector =
                spawners[0]
                    .SpawnedPlayer
                    .GetComponent<InteractionDetector>();
            Assert.That(detector, Is.Not.Null);
            Assert.That(presenters[0].IsBound, Is.True);
            Assert.That(
                presenters[0].BoundDetector,
                Is.SameAs(detector));

            return new PromptTestContext(
                spawners[0],
                detector,
                presenters[0],
                views[0]);
        }

        private static void DisableSceneTargets()
        {
            var targets =
                Object.FindObjectsByType<InteractionTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (var target in targets)
            {
                target.enabled = false;
            }
        }

        private readonly struct PromptTestContext
        {
            public PromptTestContext(
                PlayerSpawner spawner,
                InteractionDetector detector,
                InteractionPromptPresenter presenter,
                InteractionPromptView view)
            {
                Spawner = spawner;
                Detector = detector;
                Presenter = presenter;
                View = view;
            }

            public PlayerSpawner Spawner { get; }

            public InteractionDetector Detector { get; }

            public InteractionPromptPresenter Presenter { get; }

            public InteractionPromptView View { get; }
        }
    }
}
