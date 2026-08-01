using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Player.Input;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class PlayerInputReaderPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator EnabledReader_ReceivesClampedInputWithoutMovingPlayer()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var reader = spawner.SpawnedPlayer.GetComponent<PlayerInputReader>();
            var movement = spawner.SpawnedPlayer.GetComponent<PlayerMovement>();
            Assert.That(movement, Is.Not.Null);
            movement.enabled = false;
            var initialPosition = spawner.SpawnedPlayer.transform.position;

            Press(keyboard.wKey);
            Press(keyboard.dKey);
            yield return null;

            Assert.That(reader, Is.Not.Null);
            Assert.That(reader.MoveInput.x, Is.GreaterThan(0f));
            Assert.That(reader.MoveInput.y, Is.GreaterThan(0f));
            Assert.That(reader.MoveInput.magnitude, Is.LessThanOrEqualTo(1f));
            Assert.That(
                spawner.SpawnedPlayer.transform.position,
                Is.EqualTo(initialPosition));

            Release(keyboard.wKey);
            Release(keyboard.dKey);
            yield return null;

            Assert.That(reader.MoveInput, Is.EqualTo(Vector2.zero));
            Assert.That(
                spawner.SpawnedPlayer.transform.position,
                Is.EqualTo(initialPosition));
        }

        [UnityTest]
        public IEnumerator DisabledReader_ClearsInputAndReceivesInputAfterReenable()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var reader = FindReader();

            Press(keyboard.wKey);
            yield return null;
            Assert.That(reader.MoveInput, Is.Not.EqualTo(Vector2.zero));

            reader.enabled = false;
            Assert.That(reader.MoveInput, Is.EqualTo(Vector2.zero));

            Release(keyboard.wKey);
            Press(keyboard.dKey);
            yield return null;
            Assert.That(reader.MoveInput, Is.EqualTo(Vector2.zero));

            Release(keyboard.dKey);
            reader.enabled = true;
            Press(keyboard.upArrowKey);
            yield return null;

            Assert.That(reader.MoveInput, Is.EqualTo(Vector2.up));

            Release(keyboard.upArrowKey);
            yield return null;
            Assert.That(reader.MoveInput, Is.EqualTo(Vector2.zero));
        }

        [UnityTest]
        public IEnumerator RepeatedEnableCycles_EmitOneInputChangePerValue()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var reader = FindReader();
            var nonZeroChangeCount = 0;
            reader.MoveInputChanged += moveInput =>
            {
                if (moveInput != Vector2.zero)
                {
                    nonZeroChangeCount++;
                }
            };

            reader.enabled = false;
            reader.enabled = true;
            reader.enabled = false;
            reader.enabled = true;

            Press(keyboard.aKey);
            yield return null;

            Assert.That(nonZeroChangeCount, Is.EqualTo(1));
            Assert.That(reader.MoveInput, Is.EqualTo(Vector2.left));

            Release(keyboard.aKey);
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

        private static PlayerSpawner FindSpawner()
        {
            var spawners = Object.FindObjectsByType<PlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(spawners[0].SpawnedPlayer, Is.Not.Null);
            return spawners[0];
        }

        private static PlayerInputReader FindReader()
        {
            var reader = FindSpawner().SpawnedPlayer
                .GetComponent<PlayerInputReader>();

            Assert.That(reader, Is.Not.Null);
            return reader;
        }
    }
}
