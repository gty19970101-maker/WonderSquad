using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Interaction.Detection;
using WonderSquad.Level.SleepingForest.Greybox;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;
using WonderSquad.Presentation.Camera;
using WonderSquad.Puzzle.SleepingForest;
using WonderSquad.UI.Interaction;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class SleepingForestGreyboxPlayModeTests : InputTestFixture
    {
        private const string SleepingForestScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string CleanupSceneName =
            "SleepingForestGreyboxPlayModeTestCleanup";

        [UnityTest]
        public IEnumerator SleepingForest_PlayerSpawnsOnceAndCameraBinds()
        {
            yield return LoadSleepingForest();
            yield return null;

            var spawners = Object.FindObjectsByType<PlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var players = Object.FindObjectsByType<PlayerMovement>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var binders = Object.FindObjectsByType<CameraTargetBinder>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(spawners[0].SpawnedPlayer, Is.Not.Null);
            Assert.That(players, Has.Length.EqualTo(1));
            Assert.That(players[0].isActiveAndEnabled, Is.True);
            Assert.That(binders, Has.Length.EqualTo(1));
            Assert.That(binders[0].IsBound, Is.True);
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].CompareTag("MainCamera"), Is.True);

            var recoveries =
                Object.FindObjectsByType<SleepingForestFallRecovery>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            Assert.That(recoveries, Has.Length.EqualTo(1));
            Assert.That(recoveries[0].HasValidConfiguration, Is.True);
            Assert.That(
                recoveries[0].TrackedPlayer,
                Is.SameAs(spawners[0].SpawnedPlayer));

            yield return null;
            Assert.That(
                Object.FindObjectsByType<PlayerMovement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SleepingForest_FallenPlayerRecoversAndCanMove()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadSleepingForest();
            yield return null;

            var spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            var recovery =
                Object.FindFirstObjectByType<SleepingForestFallRecovery>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.SpawnedPlayer, Is.Not.Null);
            Assert.That(recovery, Is.Not.Null);
            Assert.That(recovery.TrackedPlayer, Is.SameAs(spawner.SpawnedPlayer));

            var player = spawner.SpawnedPlayer;
            var controller = player.GetComponent<CharacterController>();
            var movement = player.GetComponent<PlayerMovement>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(movement, Is.Not.Null);

            controller.enabled = false;
            player.transform.position = new Vector3(
                recovery.RecoveryPoint.position.x,
                recovery.FallYThreshold - 2f,
                recovery.RecoveryPoint.position.z);
            controller.enabled = true;

            yield return null;
            yield return null;

            Assert.That(recovery.RecoveryCount, Is.EqualTo(1));
            Assert.That(
                Vector3.Distance(
                    player.transform.position,
                    recovery.RecoveryPoint.position),
                Is.LessThan(0.5f));
            Assert.That(controller.enabled, Is.True);
            Assert.That(movement.isActiveAndEnabled, Is.True);

            var recoveredPosition = player.transform.position;
            Press(keyboard.dKey);
            yield return ObserveFrames(3);
            Release(keyboard.dKey);
            yield return null;

            Assert.That(player.transform.position.x, Is.GreaterThan(recoveredPosition.x));
            Assert.That(recovery.RecoveryCount, Is.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<PlayerMovement>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(
                Object.FindFirstObjectByType<CameraTargetBinder>().IsBound,
                Is.True);
        }

        [UnityTest]
        public IEnumerator SleepingForest_HasOnlyApprovedBeaconInteractionGameplay()
        {
            yield return LoadSleepingForest();
            yield return null;

            var targets = Object.FindObjectsByType<InteractionTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var presenters = Object.FindObjectsByType<InteractionPromptPresenter>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var controlledCameras = Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            var trials = Object.FindObjectsByType<ForestSignalTrial>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var beaconInteractions =
                Object.FindObjectsByType<ForestBeaconInteraction>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            Assert.That(targets, Has.Length.EqualTo(2));
            Assert.That(
                targets.Select(target => target.TargetId.Value),
                Is.EquivalentTo(new[]
                {
                    "sleeping_forest.beacon.a",
                    "sleeping_forest.beacon.b"
                }));
            Assert.That(trials, Has.Length.EqualTo(1));
            Assert.That(trials[0].HasValidConfiguration, Is.True);
            Assert.That(beaconInteractions, Has.Length.EqualTo(2));
            Assert.That(presenters, Has.Length.EqualTo(1));
            Assert.That(presenters[0].BoundDetector, Is.Not.Null);
            Assert.That(presenters[0].CurrentData.IsVisible, Is.False);
            Assert.That(controlledCameras, Has.Length.EqualTo(1));
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

        private static IEnumerator ObserveFrames(int frameCount)
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                yield return null;
            }
        }
    }
}
