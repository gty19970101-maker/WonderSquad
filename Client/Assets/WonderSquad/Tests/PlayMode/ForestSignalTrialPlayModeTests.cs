using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Interaction.Detection;
using WonderSquad.Level.SleepingForest.Greybox;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;
using WonderSquad.Presentation.Camera;
using WonderSquad.Puzzle.SleepingForest;
using WonderSquad.UI.Interaction;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class ForestSignalTrialPlayModeTests : InputTestFixture
    {
        private const string ScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string CleanupSceneName =
            "ForestSignalTrialPlayModeTestCleanup";

        [UnityTest]
        public IEnumerator AThenB_UsesPromptAndCompletesOnce()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadSleepingForest();
            var trial = FindTrial();
            var beaconA = FindBeacon(ForestBeaconSlot.BeaconA);
            var beaconB = FindBeacon(ForestBeaconSlot.BeaconB);
            var completionEventCount = 0;
            trial.TrialStateChanged += snapshot =>
            {
                if (snapshot.IsCompleted)
                {
                    completionEventCount++;
                }
            };

            yield return FocusBeacon(beaconA);
            AssertPromptTargets(beaconA);
            yield return PressAndRelease(keyboard.eKey);

            Assert.That(trial.CurrentSnapshot.BeaconA.IsActivated, Is.True);
            Assert.That(trial.CurrentSnapshot.BeaconB.IsActivated, Is.False);
            Assert.That(trial.CurrentSnapshot.Revision, Is.EqualTo(1U));
            Assert.That(
                beaconA.GetComponent<ForestBeaconVisual>().CurrentMode,
                Is.EqualTo(ForestBeaconVisualMode.Activated));

            yield return FocusBeacon(beaconB);
            AssertPromptTargets(beaconB);
            yield return PressAndRelease(keyboard.eKey);

            Assert.That(trial.CurrentSnapshot.IsCompleted, Is.True);
            Assert.That(trial.CurrentSnapshot.Revision, Is.EqualTo(2U));
            Assert.That(completionEventCount, Is.EqualTo(1));
            Assert.That(beaconA.IsExecutionAvailable, Is.False);
            Assert.That(beaconB.IsExecutionAvailable, Is.False);
            AssertFoundationRemainsAvailable();
            AssertRoutesRemainUnchanged();
        }

        [UnityTest]
        public IEnumerator BBeforeA_ShowsIncorrectAndRecoversThroughAThenB()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadSleepingForest();
            var trial = FindTrial();
            var beaconA = FindBeacon(ForestBeaconSlot.BeaconA);
            var beaconB = FindBeacon(ForestBeaconSlot.BeaconB);
            var visualA = beaconA.GetComponent<ForestBeaconVisual>();
            var visualB = beaconB.GetComponent<ForestBeaconVisual>();

            yield return FocusBeacon(beaconB);
            yield return PressAndRelease(keyboard.eKey);

            Assert.That(trial.CurrentSnapshot.HasIncorrectOrder, Is.True);
            Assert.That(trial.CurrentSnapshot.BeaconA.IsActivated, Is.False);
            Assert.That(trial.CurrentSnapshot.BeaconB.IsActivated, Is.False);
            Assert.That(trial.CurrentSnapshot.Revision, Is.EqualTo(1U));
            Assert.That(visualA.CurrentMode, Is.EqualTo(ForestBeaconVisualMode.Dormant));
            Assert.That(visualB.CurrentMode, Is.EqualTo(ForestBeaconVisualMode.Incorrect));
            Assert.That(beaconB.IsExecutionAvailable, Is.True);

            yield return FocusBeacon(beaconA);
            yield return PressAndRelease(keyboard.eKey);
            Assert.That(trial.CurrentSnapshot.BeaconA.IsActivated, Is.True);
            Assert.That(trial.CurrentSnapshot.BeaconB.IsActivated, Is.False);
            Assert.That(trial.CurrentSnapshot.HasIncorrectOrder, Is.False);
            Assert.That(visualB.CurrentMode, Is.EqualTo(ForestBeaconVisualMode.Dormant));

            yield return FocusBeacon(beaconB);
            yield return PressAndRelease(keyboard.eKey);
            Assert.That(trial.CurrentSnapshot.IsCompleted, Is.True);
            Assert.That(trial.CurrentSnapshot.Revision, Is.EqualTo(3U));
            Assert.That(visualA.CurrentMode, Is.EqualTo(ForestBeaconVisualMode.Activated));
            Assert.That(visualB.CurrentMode, Is.EqualTo(ForestBeaconVisualMode.Activated));
            AssertRoutesRemainUnchanged();
        }

        [UnityTest]
        public IEnumerator HeldInputAndActivatedTarget_DoNotRepeatProgress()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadSleepingForest();
            var trial = FindTrial();
            var beaconA = FindBeacon(ForestBeaconSlot.BeaconA);

            yield return FocusBeacon(beaconA);
            Press(keyboard.eKey);
            yield return null;
            var revisionAfterPress = trial.CurrentSnapshot.Revision;
            for (var frame = 0; frame < 5; frame++)
            {
                yield return null;
            }

            Release(keyboard.eKey);
            yield return null;

            Assert.That(revisionAfterPress, Is.EqualTo(1U));
            Assert.That(trial.CurrentSnapshot.Revision, Is.EqualTo(1U));
            Assert.That(trial.CurrentSnapshot.BeaconA.IsActivated, Is.True);

            yield return FocusPosition(beaconA.transform.position);
            yield return PressAndRelease(keyboard.eKey);
            Assert.That(trial.CurrentSnapshot.Revision, Is.EqualTo(1U));
            Assert.That(trial.CurrentSnapshot.BeaconA.IsActivated, Is.True);
        }

        [UnityTest]
        public IEnumerator SceneComposition_HasIndependentBeaconsAndExistingRecovery()
        {
            yield return LoadSleepingForest();
            var trial = FindTrial();
            var beacons = Object.FindObjectsByType<ForestBeaconInteraction>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(beacons, Has.Length.EqualTo(2));
            Assert.That(
                beacons.Select(beacon => beacon.TargetId.Value).Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(trial.HasValidConfiguration, Is.True);
            Assert.That(
                Object.FindObjectsByType<PlayerMovement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            var recovery =
                Object.FindFirstObjectByType<SleepingForestFallRecovery>();
            Assert.That(recovery, Is.Not.Null);
            Assert.That(recovery.HasValidConfiguration, Is.True);
            Assert.That(recovery.TrackedPlayer, Is.Not.Null);
            AssertRoutesRemainUnchanged();
        }

        [UnityTearDown]
        public IEnumerator UnloadSleepingForestAfterTest()
        {
            var sleepingForest = SceneManager.GetSceneByPath(ScenePath);
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
        }

        private static IEnumerator LoadSleepingForest()
        {
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(ScenePath));
            yield return null;
            yield return null;
        }

        private static IEnumerator FocusBeacon(
            ForestBeaconInteraction beacon)
        {
            var target = beacon.GetComponent<InteractionTarget>();
            Assert.That(target, Is.Not.Null);
            yield return FocusPosition(
                new Vector3(
                    target.DetectionPosition.x,
                    beacon.transform.position.y + 0.1f,
                    target.DetectionPosition.z - 1.2f));

            var detector = Object.FindFirstObjectByType<InteractionDetector>();
            Assert.That(detector, Is.Not.Null);
            detector.RefreshDetection();
            yield return null;
            Assert.That(detector.CurrentTarget, Is.Not.Null);
            Assert.That(detector.CurrentTarget.TargetId, Is.EqualTo(beacon.TargetId));
        }

        private static IEnumerator FocusPosition(Vector3 position)
        {
            var spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.SpawnedPlayer, Is.Not.Null);
            var player = spawner.SpawnedPlayer;
            var controller = player.GetComponent<CharacterController>();
            Assert.That(controller, Is.Not.Null);
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
            Physics.SyncTransforms();
            yield return null;
        }

        private IEnumerator PressAndRelease(KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private static ForestSignalTrial FindTrial()
        {
            var trials = Object.FindObjectsByType<ForestSignalTrial>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Assert.That(trials, Has.Length.EqualTo(1));
            return trials[0];
        }

        private static ForestBeaconInteraction FindBeacon(
            ForestBeaconSlot slot)
        {
            var beacon = Object.FindObjectsByType<ForestBeaconInteraction>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .SingleOrDefault(candidate => candidate.Slot == slot);
            Assert.That(beacon, Is.Not.Null);
            return beacon;
        }

        private static void AssertPromptTargets(
            ForestBeaconInteraction beacon)
        {
            var presenter =
                Object.FindFirstObjectByType<InteractionPromptPresenter>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(presenter.CurrentData.IsVisible, Is.True);
            Assert.That(
                presenter.CurrentData.TargetId,
                Is.EqualTo(beacon.TargetId));
            Assert.That(
                presenter.CurrentData.ActionText,
                Is.Not.Empty);
        }

        private static void AssertFoundationRemainsAvailable()
        {
            var movement = Object.FindFirstObjectByType<PlayerMovement>();
            var binder = Object.FindFirstObjectByType<CameraTargetBinder>();
            var recovery =
                Object.FindFirstObjectByType<SleepingForestFallRecovery>();
            Assert.That(movement, Is.Not.Null);
            Assert.That(movement.isActiveAndEnabled, Is.True);
            Assert.That(binder, Is.Not.Null);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(recovery, Is.Not.Null);
            Assert.That(recovery.HasValidConfiguration, Is.True);
        }

        private static void AssertRoutesRemainUnchanged()
        {
            AssertRouteColliderEnabled("MainRoute_ToBeaconA");
            AssertRouteColliderEnabled("AdvantageRouteHighPlatform");
            AssertRouteColliderEnabled("RootBridgeTemporaryCrossing");
            Assert.That(
                GameObject.Find("RootBridgeGameplay"),
                Is.Null);
            Assert.That(
                GameObject.Find("CompletionGameplay"),
                Is.Null);
        }

        private static void AssertRouteColliderEnabled(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            Assert.That(gameObject, Is.Not.Null, objectName);
            var collider = gameObject.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null, objectName);
            Assert.That(collider.enabled, Is.True, objectName);
        }
    }
}
