using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Level.SleepingForest.Greybox;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;
using WonderSquad.Presentation.Camera;
using WonderSquad.Puzzle.SleepingForest;
using WonderSquad.SleepingForest.Completion;
using WonderSquad.UI.Interaction;
using WonderSquad.UI.SleepingForest;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class SleepingForestCompletionPlayModeTests
    {
        private const string SleepingForestScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string CleanupSceneName =
            "SleepingForestCompletionPlayModeTestCleanup";
        private const int TriggerObservationFrames = 12;

        [UnityTest]
        public IEnumerator IncompleteTrialAtEnd_ShowsRequirementAndCanLeave()
        {
            yield return LoadSleepingForest();
            var context = FindSceneContext();

            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center,
                true);

            Assert.That(
                context.Controller.CurrentSnapshot.IsCompleted,
                Is.False);
            Assert.That(
                context.Controller.FeedbackState,
                Is.EqualTo(
                    SleepingForestCompletionFeedbackState.ConditionsUnmet));
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.View.DisplayedMessage,
                Is.EqualTo(
                    SleepingForestCompletionPresenter
                        .RequirementFallbackMessage));

            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center +
                Vector3.back * 8f,
                false);

            Assert.That(
                context.Controller.FeedbackState,
                Is.EqualTo(
                    SleepingForestCompletionFeedbackState.Hidden));
            Assert.That(context.View.IsVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator CompletedTrialThenEnd_CompletesOnceWithFeedback()
        {
            yield return LoadSleepingForest();
            var context = FindSceneContext();
            var completionCount = 0;
            context.Controller.Completed += snapshot => completionCount++;

            CompleteTrial(context.Trial);
            Assert.That(context.Trial.CurrentSnapshot.IsCompleted, Is.True);
            Assert.That(
                context.Controller.CurrentSnapshot.IsCompleted,
                Is.False);

            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center,
                true);

            AssertCompletion(context);
            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<
                    SleepingForestCompletionController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<SleepingForestSliceEndTrigger>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));

            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center +
                Vector3.back * 8f,
                false);
            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center,
                true);

            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(
                context.Controller.CurrentSnapshot.CompletionRevision,
                Is.EqualTo(1U));
        }

        [UnityTest]
        public IEnumerator PlayerInsideEndThenTrialCompletes_CompletesImmediately()
        {
            yield return LoadSleepingForest();
            var context = FindSceneContext();

            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center,
                true);
            Assert.That(
                context.Controller.CurrentSnapshot.IsCompleted,
                Is.False);

            CompleteTrial(context.Trial);
            yield return null;

            AssertCompletion(context);
            Assert.That(context.Trigger.IsPlayerPresent, Is.True);
        }

        [UnityTest]
        public IEnumerator IncorrectOrderRecoversWithoutFoundationRegression()
        {
            yield return LoadSleepingForest();
            var context = FindSceneContext();
            yield return MovePlayerTo(
                context,
                context.Trigger.TriggerCollider.bounds.center,
                true);

            Execute(context.Trial.BeaconB, 1U);
            Assert.That(
                context.Trial.CurrentSnapshot.HasIncorrectOrder,
                Is.True);
            Assert.That(
                context.Controller.CurrentSnapshot.IsCompleted,
                Is.False);

            Execute(context.Trial.BeaconA, 2U);
            Execute(context.Trial.BeaconB, 3U);
            yield return null;

            AssertCompletion(context);
            Assert.That(
                Object.FindObjectsByType<PlayerMovement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindFirstObjectByType<CameraTargetBinder>().IsBound,
                Is.True);
            Assert.That(
                Object.FindFirstObjectByType<InteractionPromptPresenter>(),
                Is.Not.Null);
            Assert.That(
                Object.FindFirstObjectByType<SleepingForestFallRecovery>()
                    .HasValidConfiguration,
                Is.True);
        }

        private static SceneContext FindSceneContext()
        {
            var context = new SceneContext(
                Object.FindFirstObjectByType<PlayerSpawner>(),
                Object.FindFirstObjectByType<ForestSignalTrial>(),
                Object.FindFirstObjectByType<
                    SleepingForestSliceEndTrigger>(),
                Object.FindFirstObjectByType<
                    SleepingForestCompletionController>(),
                Object.FindFirstObjectByType<
                    SleepingForestCompletionView>(),
                Object.FindFirstObjectByType<
                    SleepingForestCompletionMarkerView>());

            Assert.That(context.Spawner, Is.Not.Null);
            Assert.That(context.Spawner.SpawnedPlayer, Is.Not.Null);
            Assert.That(context.Trial, Is.Not.Null);
            Assert.That(context.Trigger, Is.Not.Null);
            Assert.That(context.Trigger.HasValidConfiguration, Is.True);
            Assert.That(context.Controller, Is.Not.Null);
            Assert.That(context.Controller.HasValidConfiguration, Is.True);
            Assert.That(context.View, Is.Not.Null);
            Assert.That(context.MarkerView, Is.Not.Null);
            return context;
        }

        private static IEnumerator MovePlayerTo(
            SceneContext context,
            Vector3 position,
            bool expectedPresence)
        {
            var player = context.Spawner.SpawnedPlayer;
            var characterController =
                player.GetComponent<CharacterController>();
            Assert.That(characterController, Is.Not.Null);

            if (!expectedPresence && context.Trigger.IsPlayerPresent)
            {
                characterController.Move(
                    position - player.transform.position);
            }
            else
            {
                characterController.enabled = false;
                player.transform.position = position;
                characterController.enabled = true;
            }

            Physics.SyncTransforms();

            for (var frame = 0;
                 frame < TriggerObservationFrames &&
                 context.Trigger.IsPlayerPresent != expectedPresence;
                 frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(
                context.Trigger.IsPlayerPresent,
                Is.EqualTo(expectedPresence));
        }

        private static void CompleteTrial(ForestSignalTrial trial)
        {
            Execute(trial.BeaconA, 1U);
            Execute(trial.BeaconB, 2U);
        }

        private static void Execute(
            ForestBeaconInteraction beacon,
            uint requestId)
        {
            var result = beacon.Execute(new InteractionContext(
                new PlayerId(1UL),
                new RequestId(requestId),
                requestId,
                beacon.transform.position,
                beacon.transform.forward));
            Assert.That(
                result.Code,
                Is.EqualTo(InteractionResultCode.Success));
        }

        private static void AssertCompletion(SceneContext context)
        {
            Assert.That(
                context.Controller.CurrentSnapshot.State,
                Is.EqualTo(SleepingForestCompletionState.Completed));
            Assert.That(
                context.Controller.CurrentSnapshot.CompletionRevision,
                Is.EqualTo(1U));
            Assert.That(
                context.Controller.CurrentSnapshot
                    .TrialRevisionAtCompletion,
                Is.EqualTo(context.Trial.CurrentSnapshot.Revision));
            Assert.That(
                context.Controller.FeedbackState,
                Is.EqualTo(
                    SleepingForestCompletionFeedbackState.Completed));
            Assert.That(context.View.IsVisible, Is.True);
            Assert.That(
                context.View.DisplayedMessage,
                Is.EqualTo(
                    SleepingForestCompletionPresenter
                        .CompletionFallbackMessage));
            Assert.That(
                context.MarkerView.IsCompletedMarkerVisible,
                Is.True);
        }

        private static IEnumerator LoadSleepingForest()
        {
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                SleepingForestScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(operation, Is.Not.Null);

            while (!operation.isDone)
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().path,
                Is.EqualTo(SleepingForestScenePath));
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnloadSleepingForestAfterTest()
        {
            var sleepingForest =
                SceneManager.GetSceneByPath(SleepingForestScenePath);
            if (!sleepingForest.IsValid() || !sleepingForest.isLoaded)
            {
                yield break;
            }

            var cleanupScene = SceneManager.CreateScene(CleanupSceneName);
            SceneManager.SetActiveScene(cleanupScene);
            var unloadOperation =
                SceneManager.UnloadSceneAsync(sleepingForest);
            Assert.That(unloadOperation, Is.Not.Null);
            while (!unloadOperation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private readonly struct SceneContext
        {
            public SceneContext(
                PlayerSpawner spawner,
                ForestSignalTrial trial,
                SleepingForestSliceEndTrigger trigger,
                SleepingForestCompletionController controller,
                SleepingForestCompletionView view,
                SleepingForestCompletionMarkerView markerView)
            {
                Spawner = spawner;
                Trial = trial;
                Trigger = trigger;
                Controller = controller;
                View = view;
                MarkerView = markerView;
            }

            public PlayerSpawner Spawner { get; }

            public ForestSignalTrial Trial { get; }

            public SleepingForestSliceEndTrigger Trigger { get; }

            public SleepingForestCompletionController Controller { get; }

            public SleepingForestCompletionView View { get; }

            public SleepingForestCompletionMarkerView MarkerView { get; }
        }
    }
}
