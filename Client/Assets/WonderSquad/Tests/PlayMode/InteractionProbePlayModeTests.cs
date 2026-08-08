using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Interaction.Execution;
using WonderSquad.Player.Spawning;
using WonderSquad.UI.Interaction;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class InteractionProbePlayModeTests :
        InputTestFixture
    {
        private const string InteractableLayerName = "Interactable";
        private const float HorizontalPositionTolerance = 0.001f;

        [UnityTest]
        public IEnumerator PlayerNearProbe_ShowsPromptAndTogglesOncePerPress()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            var probe = PrepareProbe(context);

            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.Presenter.CurrentData.TargetId,
                Is.EqualTo(probe.TargetId));
            Assert.That(probe.IsActive, Is.False);

            Press(keyboard.eKey);
            yield return null;
            yield return null;

            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
            Assert.That(probe.IsActive, Is.True);
            Assert.That(
                probe.GetComponent<InteractionProbeVisualState>()
                    .IsDisplayingActive,
                Is.True);
            Assert.That(
                context.Executor.LastResult.Code,
                Is.EqualTo(InteractionResultCode.Success));

            yield return null;
            Assert.That(probe.ExecutionCount, Is.EqualTo(1));

            Release(keyboard.eKey);
            yield return null;
            Press(keyboard.eKey);
            yield return null;

            Assert.That(probe.ExecutionCount, Is.EqualTo(2));
            Assert.That(probe.IsActive, Is.False);
            Assert.That(
                probe.GetComponent<InteractionProbeVisualState>()
                    .IsDisplayingActive,
                Is.False);

            Release(keyboard.eKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Probe_WhenOccludedOrDestroyed_DoesNotExecuteAndRemainsSafe()
        {
            yield return LoadPlayerSandbox();

            var context = FindContext();
            var probe = PrepareProbe(context);
            var origin = context.Detector.DetectionOrigin.position;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.layer = 0;
            wall.transform.position = Vector3.Lerp(
                origin,
                probe.transform.position,
                0.5f);
            wall.transform.localScale = new Vector3(0.25f, 2f, 1f);
            Physics.SyncTransforms();

            var occludedResult = context.Executor.ExecuteCurrentTarget();
            Assert.That(
                occludedResult.Code,
                Is.EqualTo(InteractionResultCode.Occluded));
            Assert.That(probe.ExecutionCount, Is.EqualTo(0));

            Object.Destroy(wall);
            Object.Destroy(probe.gameObject);
            yield return null;
            Physics.SyncTransforms();
            context.Detector.RefreshDetection();

            Assert.That(context.View.IsVisible, Is.False);
            Assert.That(
                context.Executor.ExecuteCurrentTarget().Code,
                Is.EqualTo(InteractionResultCode.TargetInvalid));
        }

        [UnityTest]
        public IEnumerator ProbeExecution_DoesNotMovePlayerAndPreservesMainCamera()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();

            var context = FindContext();
            var probe = PrepareProbe(context);
            var initialPosition = context.Spawner.SpawnedPlayer.transform.position;

            Press(keyboard.eKey);
            yield return null;
            Release(keyboard.eKey);
            yield return null;

            var finalPosition = context.Spawner.SpawnedPlayer.transform.position;
            Assert.That(probe.ExecutionCount, Is.EqualTo(1));
            Assert.That(
                Mathf.Abs(finalPosition.x - initialPosition.x),
                Is.LessThanOrEqualTo(HorizontalPositionTolerance));
            Assert.That(
                Mathf.Abs(finalPosition.z - initialPosition.z),
                Is.LessThanOrEqualTo(HorizontalPositionTolerance));
            Assert.That(Camera.main, Is.Not.Null);
        }

        private static InteractionProbeBehaviour PrepareProbe(
            InteractionProbeTestContext context)
        {
            var probes = Object.FindObjectsByType<InteractionProbeBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(probes, Has.Length.EqualTo(1));
            var probe = probes[0];

            DisableCompetingSceneTargets(probe.TargetId);
            probe.transform.position =
                context.Detector.DetectionOrigin.position +
                Vector3.right;
            Physics.SyncTransforms();

            Assert.That(context.Detector.RefreshDetection(), Is.True);
            Assert.That(
                context.Detector.CurrentTarget.TargetId,
                Is.EqualTo(probe.TargetId));
            return probe;
        }

        private static void DisableCompetingSceneTargets(
            InteractionTargetId allowedTargetId)
        {
            var targets = Object.FindObjectsByType<InteractionTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var target in targets)
            {
                if (target.TargetId != allowedTargetId)
                {
                    target.enabled = false;
                }
            }

            var executionProbes =
                Object.FindObjectsByType<InteractionExecutionProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (var executionProbe in executionProbes)
            {
                executionProbe.enabled = false;
            }
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

        private static InteractionProbeTestContext FindContext()
        {
            var spawners = Object.FindObjectsByType<PlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var presenters = Object.FindObjectsByType<InteractionPromptPresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var views = Object.FindObjectsByType<InteractionPromptView>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(presenters, Has.Length.EqualTo(1));
            Assert.That(views, Has.Length.EqualTo(1));
            Assert.That(spawners[0].SpawnedPlayer, Is.Not.Null);

            var player = spawners[0].SpawnedPlayer;
            var detector = player.GetComponent<InteractionDetector>();
            var executor = player.GetComponent<InteractionExecutor>();

            Assert.That(detector, Is.Not.Null);
            Assert.That(executor, Is.Not.Null);
            Assert.That(executor.HasValidConfiguration, Is.True);
            Assert.That(
                presenters[0].BoundDetector,
                Is.SameAs(detector));

            return new InteractionProbeTestContext(
                spawners[0],
                detector,
                executor,
                presenters[0],
                views[0]);
        }

        private readonly struct InteractionProbeTestContext
        {
            public InteractionProbeTestContext(
                PlayerSpawner spawner,
                InteractionDetector detector,
                InteractionExecutor executor,
                InteractionPromptPresenter presenter,
                InteractionPromptView view)
            {
                Spawner = spawner;
                Detector = detector;
                Executor = executor;
                Presenter = presenter;
                View = view;
            }

            public PlayerSpawner Spawner { get; }

            public InteractionDetector Detector { get; }

            public InteractionExecutor Executor { get; }

            public InteractionPromptPresenter Presenter { get; }

            public InteractionPromptView View { get; }
        }
    }
}
