using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class PlayerMovementPlayModeTests : InputTestFixture
    {
        private const int MovementObservationFrameCount = 3;
        private const int FallingObservationFrameCount = 5000;
        private const float PositionTolerance = 0.001f;

        [UnityTest]
        public IEnumerator MoveInput_MovesPlayerAndRotatesTowardsDirection()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var movement = GetMovement(spawner);
            var initialPosition = spawner.SpawnedPlayer.transform.position;

            Press(keyboard.dKey);
            yield return ObserveFrames(MovementObservationFrameCount);

            Assert.That(
                spawner.SpawnedPlayer.transform.position.x,
                Is.GreaterThan(initialPosition.x));
            Assert.That(movement.HorizontalVelocity.x, Is.GreaterThan(0f));
            Assert.That(
                Vector3.Dot(
                    spawner.SpawnedPlayer.transform.forward,
                    Vector3.right),
                Is.GreaterThan(0f));

            Release(keyboard.dKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReleasedInput_StopsHorizontalMovementWithoutDrift()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var movement = GetMovement(spawner);

            Press(keyboard.wKey);
            yield return ObserveFrames(MovementObservationFrameCount);
            Release(keyboard.wKey);
            yield return null;

            var stoppedPosition = spawner.SpawnedPlayer.transform.position;
            yield return ObserveFrames(MovementObservationFrameCount);
            var finalPosition = spawner.SpawnedPlayer.transform.position;

            Assert.That(movement.HorizontalVelocity, Is.EqualTo(Vector3.zero));
            Assert.That(
                finalPosition.x,
                Is.EqualTo(stoppedPosition.x).Within(PositionTolerance));
            Assert.That(
                finalPosition.z,
                Is.EqualTo(stoppedPosition.z).Within(PositionTolerance));
        }

        [UnityTest]
        public IEnumerator Gravity_LandsPlayerAndReportsGroundedWithoutPenetration()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var movement = GetMovement(spawner);
            var controller =
                spawner.SpawnedPlayer.GetComponent<CharacterController>();
            var elevatedPosition =
                spawner.SpawnedPlayer.transform.position + Vector3.up * 3f;

            controller.enabled = false;
            spawner.SpawnedPlayer.transform.position = elevatedPosition;
            controller.enabled = true;

            yield return null;
            var fallingPosition = spawner.SpawnedPlayer.transform.position;
            Assert.That(fallingPosition.y, Is.LessThan(elevatedPosition.y));

            for (var frame = 0;
                 frame < FallingObservationFrameCount && !movement.IsGrounded;
                 frame++)
            {
                yield return null;
            }

            Assert.That(
                movement.IsGrounded,
                Is.True,
                $"Position: {spawner.SpawnedPlayer.transform.position}, " +
                $"Controller grounded: {controller.isGrounded}, " +
                $"Vertical velocity: {movement.VerticalVelocity}");
            Assert.That(
                spawner.SpawnedPlayer.transform.position.y,
                Is.GreaterThanOrEqualTo(-PositionTolerance));
        }

        [UnityTest]
        public IEnumerator DisabledMovement_DoesNotMoveAndSpawnerRemainsStable()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var spawnedPlayer = spawner.SpawnedPlayer;
            var movement = GetMovement(spawner);
            movement.enabled = false;
            var initialPosition = spawnedPlayer.transform.position;

            Press(keyboard.wKey);
            yield return ObserveFrames(MovementObservationFrameCount);

            Assert.That(spawnedPlayer.transform.position, Is.EqualTo(initialPosition));
            Assert.That(spawner.SpawnedPlayer, Is.SameAs(spawnedPlayer));
            Assert.That(CountSpawnedPlayerRoots(spawner), Is.EqualTo(1));

            Release(keyboard.wKey);
            yield return null;
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
            Assert.That(spawners[0].SpawnedPlayer, Is.Not.Null);
            return spawners[0];
        }

        private static PlayerMovement GetMovement(PlayerSpawner spawner)
        {
            var movement =
                spawner.SpawnedPlayer.GetComponent<PlayerMovement>();

            Assert.That(movement, Is.Not.Null);
            Assert.That(movement.HasValidConfiguration, Is.True);
            return movement;
        }

        private static int CountSpawnedPlayerRoots(PlayerSpawner spawner)
        {
            var expectedName = spawner.PlayerPrefab.name + "(Clone)";
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var matchingRootCount = 0;

            foreach (var root in roots)
            {
                if (root.name == expectedName)
                {
                    matchingRootCount++;
                }
            }

            return matchingRootCount;
        }
    }
}
