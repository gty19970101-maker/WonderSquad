using System.Collections;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Player.Spawning;
using WonderSquad.Presentation.Camera;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class CameraFollowPlayModeTests : InputTestFixture
    {
        private const int MovementFrameCount = 12;
        private const int SettlingFrameCount = 60;
        private const int StabilityFrameCount = 12;
        private const float PositionTolerance = 0.02f;
        private const float RotationToleranceDegrees = 0.1f;

        [UnityTest]
        public IEnumerator PlayerSpawnedAfterCameraInitialization_BindsTarget()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var binder = FindBinder();

            Assert.That(spawner.SpawnedPlayer, Is.Not.Null);
            Assert.That(binder.HasValidConfiguration, Is.True);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(binder.BoundPlayer, Is.SameAs(spawner.SpawnedPlayer));
            Assert.That(binder.TrackingTarget, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator ExistingPlayer_WhenBinderReenabled_RebindsImmediately()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var binder = FindBinder();
            binder.enabled = false;
            yield return null;

            Assert.That(binder.IsBound, Is.False);
            binder.enabled = true;
            yield return null;

            Assert.That(binder.IsBound, Is.True);
            Assert.That(binder.BoundPlayer, Is.SameAs(spawner.SpawnedPlayer));
        }

        [UnityTest]
        public IEnumerator PlayerMovement_CameraFollowsAndSettlesWithoutDrift()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var mainCamera = FindMainCamera();
            var initialPosition = mainCamera.transform.position;

            Press(keyboard.dKey);
            yield return ObserveFrames(MovementFrameCount);
            Release(keyboard.dKey);
            yield return ObserveFrames(SettlingFrameCount);

            Assert.That(
                mainCamera.transform.position.x,
                Is.GreaterThan(initialPosition.x));

            var settledPosition = mainCamera.transform.position;
            yield return ObserveFrames(StabilityFrameCount);

            Assert.That(
                Vector3.Distance(
                    mainCamera.transform.position,
                    settledPosition),
                Is.LessThan(PositionTolerance));
        }

        [UnityTest]
        public IEnumerator PlayerRotation_DoesNotOrbitControlledCamera()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var controlledCamera = FindControlledCamera();
            var initialRotation = controlledCamera.transform.rotation;

            spawner.SpawnedPlayer.transform.rotation =
                Quaternion.Euler(0f, 180f, 0f);
            yield return ObserveFrames(MovementFrameCount);

            Assert.That(
                Quaternion.Angle(
                    initialRotation,
                    controlledCamera.transform.rotation),
                Is.LessThan(RotationToleranceDegrees));
        }

        [UnityTest]
        public IEnumerator DestroyedAndRespawnedPlayer_RebindsWithoutDuplicates()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var binder = FindBinder();
            var firstPlayer = spawner.SpawnedPlayer;

            Object.Destroy(firstPlayer);
            yield return null;
            yield return null;

            Assert.That(binder.IsBound, Is.False);
            Assert.That(binder.TrackingTarget, Is.Null);
            Assert.That(spawner.TrySpawn(out var secondPlayer), Is.True);
            yield return null;

            Assert.That(secondPlayer, Is.Not.Null);
            Assert.That(secondPlayer, Is.Not.SameAs(firstPlayer));
            Assert.That(binder.IsBound, Is.True);
            Assert.That(binder.BoundPlayer, Is.SameAs(secondPlayer));
            Assert.That(CountMainCameras(), Is.EqualTo(1));
            Assert.That(CountControlledCameras(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator PlayerSandbox_HasSingleValidCameraRig()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            Assert.That(CountMainCameras(), Is.EqualTo(1));
            Assert.That(CountControlledCameras(), Is.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<CinemachineBrain>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(FindMainCamera().CompareTag("MainCamera"), Is.True);
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
        }

        private static IEnumerator ObserveFrames(int frameCount)
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                yield return null;
            }
        }

        private static PlayerSpawner FindSpawner()
        {
            var spawners = Object.FindObjectsByType<PlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            return spawners[0];
        }

        private static CameraTargetBinder FindBinder()
        {
            var binders = Object.FindObjectsByType<CameraTargetBinder>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(binders, Has.Length.EqualTo(1));
            return binders[0];
        }

        private static UnityEngine.Camera FindMainCamera()
        {
            var mainCameras = Object.FindObjectsByType<UnityEngine.Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            Assert.That(mainCameras, Has.Length.EqualTo(1));
            return mainCameras[0];
        }

        private static CinemachineCamera FindControlledCamera()
        {
            var controlledCameras =
                Object.FindObjectsByType<CinemachineCamera>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            Assert.That(controlledCameras, Has.Length.EqualTo(1));
            return controlledCameras[0];
        }

        private static int CountMainCameras()
        {
            return Object.FindObjectsByType<UnityEngine.Camera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).Length;
        }

        private static int CountControlledCameras()
        {
            return Object.FindObjectsByType<CinemachineCamera>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None).Length;
        }
    }
}
