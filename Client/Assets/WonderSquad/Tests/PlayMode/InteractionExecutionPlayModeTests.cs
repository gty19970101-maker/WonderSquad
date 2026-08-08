using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Interaction.Execution;
using WonderSquad.Interaction.Prompt;
using WonderSquad.Player.Input;
using WonderSquad.Player.Spawning;
using WonderSquad.UI.Interaction;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class InteractionExecutionPlayModeTests :
        InputTestFixture
    {
        private const string InteractableLayerName = "Interactable";
        private const float HorizontalPositionTolerance = 0.001f;

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
        public IEnumerator KeyboardPress_ExecutesOnceAndReleaseRearms()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var probe = CreateProbe(
                "interaction.execution.keyboard",
                context.Detector.DetectionOrigin.position +
                Vector3.right,
                true);
            Physics.SyncTransforms();
            Assert.That(context.Detector.RefreshDetection(), Is.True);
            Assert.That(context.View.IsVisible, Is.True);

            Press(keyboard.eKey);
            yield return null;
            yield return null;
            yield return null;

            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
            Assert.That(probe.IsProbeStateActive, Is.True);
            Assert.That(
                context.Executor.LastResult.Code,
                Is.EqualTo(InteractionResultCode.Success));

            Release(keyboard.eKey);
            yield return null;
            Press(keyboard.eKey);
            yield return null;

            Assert.That(probe.ExecutionCount, Is.EqualTo(2));
            Assert.That(probe.IsProbeStateActive, Is.False);

            Release(keyboard.eKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GamepadPress_ExecutesOnce()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var probe = CreateProbe(
                "interaction.execution.gamepad",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            Press(gamepad.buttonWest);
            yield return null;
            yield return null;

            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
            Assert.That(
                context.Executor.LastResult.Code,
                Is.EqualTo(InteractionResultCode.Success));

            Release(gamepad.buttonWest);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MissingUnsupportedAndDestroyedTargets_DoNotExecute()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            context.Detector.RefreshDetection();

            var noTargetResult =
                context.Executor.ExecuteCurrentTarget();
            Assert.That(
                noTargetResult.Code,
                Is.EqualTo(InteractionResultCode.TargetInvalid));

            var unsupportedObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            unsupportedObject.layer =
                LayerMask.NameToLayer(InteractableLayerName);
            unsupportedObject.transform.position =
                context.Detector.DetectionOrigin.position +
                Vector3.right;
            unsupportedObject.GetComponent<Collider>().isTrigger = true;
            var unsupportedTarget =
                unsupportedObject.AddComponent<InteractionTarget>();
            unsupportedTarget.Configure(
                "interaction.execution.unsupported",
                unsupportedObject.transform,
                true,
                0);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.NotSupported));

            Object.Destroy(unsupportedObject);
            yield return null;
            Physics.SyncTransforms();

            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.TargetInvalid));
        }

        [UnityTest]
        public IEnumerator RevalidationRejectsRangeLayerAndOcclusionChanges()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var originPosition =
                context.Detector.DetectionOrigin.position;
            var probe = CreateProbe(
                "interaction.execution.revalidation",
                originPosition + Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            probe.transform.position =
                originPosition +
                Vector3.right *
                (context.Detector.Settings.DetectionRadius + 1f);
            Physics.SyncTransforms();
            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.OutOfRange));
            Assert.That(probe.ExecutionCount, Is.EqualTo(0));

            probe.transform.position = originPosition + Vector3.right;
            probe.gameObject.layer = 0;
            Physics.SyncTransforms();
            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.LayerRejected));
            Assert.That(probe.ExecutionCount, Is.EqualTo(0));

            probe.gameObject.layer =
                LayerMask.NameToLayer(InteractableLayerName);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "InteractionExecutionOcclusionWall";
            wall.layer = 0;
            wall.transform.position =
                Vector3.Lerp(
                    originPosition,
                    probe.DetectionPosition,
                    0.5f);
            wall.transform.localScale =
                new Vector3(0.25f, 2f, 1f);
            Physics.SyncTransforms();

            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.Occluded));
            Assert.That(probe.ExecutionCount, Is.EqualTo(0));

            wall.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.Success));
            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisabledInputOrExecutor_DoesNotExecuteAndReenableIsStable()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var probe = CreateProbe(
                "interaction.execution.lifecycle",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            context.InputReader.enabled = false;
            Press(keyboard.eKey);
            yield return null;
            Assert.That(probe.ExecutionCount, Is.EqualTo(0));

            Release(keyboard.eKey);
            context.InputReader.enabled = true;
            context.Executor.enabled = false;
            Press(keyboard.eKey);
            yield return null;
            Assert.That(probe.ExecutionCount, Is.EqualTo(0));

            Release(keyboard.eKey);
            context.Executor.enabled = true;
            Press(keyboard.eKey);
            yield return null;
            Assert.That(probe.ExecutionCount, Is.EqualTo(1));

            Release(keyboard.eKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DuplicateRequest_DoesNotApplyProbeTwice()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var probe = CreateProbe(
                "interaction.execution.duplicate",
                context.Detector.DetectionOrigin.position +
                Vector3.right);
            var request = new InteractionRequest(
                new InteractionContext(
                    new PlayerId(1UL),
                    new RequestId(99U),
                    1d,
                    context.Detector.DetectionOrigin.position,
                    Vector3.forward),
                probe.TargetId);

            var first = context.RequestPort.Execute(request, probe);
            var second = context.RequestPort.Execute(request, probe);

            Assert.That(first.Code, Is.EqualTo(InteractionResultCode.Success));
            Assert.That(second.Code, Is.EqualTo(first.Code));
            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator InteractionExecution_DoesNotMovePlayerOrChangePromptTarget()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            DisableSceneTargets();
            var probe = CreateProbe(
                "interaction.execution.read_only_prompt",
                context.Detector.DetectionOrigin.position +
                Vector3.right,
                true);
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();
            var initialPosition =
                context.Spawner.SpawnedPlayer.transform.position;
            var promptTargetBefore =
                context.Presenter.CurrentData.TargetId;

            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return null;

            var finalPosition =
                context.Spawner.SpawnedPlayer.transform.position;
            Assert.That(
                Mathf.Abs(finalPosition.x - initialPosition.x),
                Is.LessThanOrEqualTo(HorizontalPositionTolerance));
            Assert.That(
                Mathf.Abs(finalPosition.z - initialPosition.z),
                Is.LessThanOrEqualTo(HorizontalPositionTolerance));
            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.Presenter.CurrentData.TargetId,
                Is.EqualTo(promptTargetBefore));
            Assert.That(
                context.Presenter.CurrentData.TargetId,
                Is.EqualTo(probe.TargetId));
        }

        private InteractionExecutionProbe CreateProbe(
            string targetId,
            Vector3 position,
            bool shouldAddPrompt = false)
        {
            var probeObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            probeObject.name = targetId;
            probeObject.layer =
                LayerMask.NameToLayer(InteractableLayerName);
            probeObject.transform.position = position;
            probeObject.GetComponent<Collider>().isTrigger = true;

            var probe =
                probeObject.AddComponent<InteractionExecutionProbe>();
            Assert.That(
                probe.Configure(
                    targetId,
                    probeObject.transform,
                    true,
                    true,
                    0,
                    InteractionResultCode.Success),
                Is.True);

            if (!shouldAddPrompt)
            {
                return probe;
            }

            var definition =
                ScriptableObject
                    .CreateInstance<InteractionPromptDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            Assert.That(
                definition.Configure(
                    $"{targetId}.prompt",
                    $"{targetId}.action",
                    "Interact"),
                Is.True);
            createdDefinitions.Add(definition);
            var promptSource =
                probeObject.AddComponent<InteractionPromptSource>();
            Assert.That(promptSource.Configure(definition), Is.True);
            return probe;
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

        private static InteractionTestContext FindContext()
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

            var player = spawners[0].SpawnedPlayer;
            var detector = player.GetComponent<InteractionDetector>();
            var executor = player.GetComponent<InteractionExecutor>();
            var inputReader =
                player.GetComponent<PlayerInteractionInputReader>();
            var requestPort =
                player.GetComponent<LocalInteractionRequestPort>();

            Assert.That(detector, Is.Not.Null);
            Assert.That(executor, Is.Not.Null);
            Assert.That(executor.HasValidConfiguration, Is.True);
            Assert.That(inputReader, Is.Not.Null);
            Assert.That(inputReader.HasValidConfiguration, Is.True);
            Assert.That(requestPort, Is.Not.Null);
            Assert.That(presenters[0].BoundDetector, Is.SameAs(detector));

            return new InteractionTestContext(
                spawners[0],
                detector,
                executor,
                inputReader,
                requestPort,
                presenters[0],
                views[0]);
        }

        private static void DisableSceneTargets()
        {
            var targets = Object.FindObjectsByType<InteractionTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                target.enabled = false;
            }

            var probes =
                Object.FindObjectsByType<InteractionExecutionProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (var probe in probes)
            {
                probe.enabled = false;
            }
        }

        private readonly struct InteractionTestContext
        {
            public InteractionTestContext(
                PlayerSpawner spawner,
                InteractionDetector detector,
                InteractionExecutor executor,
                PlayerInteractionInputReader inputReader,
                LocalInteractionRequestPort requestPort,
                InteractionPromptPresenter presenter,
                InteractionPromptView view)
            {
                Spawner = spawner;
                Detector = detector;
                Executor = executor;
                InputReader = inputReader;
                RequestPort = requestPort;
                Presenter = presenter;
                View = view;
            }

            public PlayerSpawner Spawner { get; }

            public InteractionDetector Detector { get; }

            public InteractionExecutor Executor { get; }

            public PlayerInteractionInputReader InputReader { get; }

            public LocalInteractionRequestPort RequestPort { get; }

            public InteractionPromptPresenter Presenter { get; }

            public InteractionPromptView View { get; }
        }
    }
}
