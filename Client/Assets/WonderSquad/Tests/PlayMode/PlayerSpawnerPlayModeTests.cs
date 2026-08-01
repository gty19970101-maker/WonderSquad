using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class PlayerSpawnerPlayModeTests
    {
        [UnityTest]
        public IEnumerator Start_WhenShouldSpawnOnStartIsTrue_SpawnsOnePlayer()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();

            Assert.That(spawner.SpawnedPlayer, Is.Not.Null);
            Assert.That(CountSpawnedPlayerRoots(spawner), Is.EqualTo(1));
            Assert.That(
                spawner.SpawnedPlayer.transform.position,
                Is.EqualTo(spawner.SpawnPoint.Position));
            Assert.That(
                spawner.SpawnedPlayer.transform.rotation,
                Is.EqualTo(spawner.SpawnPoint.Rotation));
        }

        [UnityTest]
        public IEnumerator StartThenRepeatedTrySpawn_DoesNotSpawnDuplicate()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var firstPlayer = spawner.SpawnedPlayer;
            var didSpawnFirst = spawner.TrySpawn(out var firstReturnedPlayer);
            var didSpawnSecond = spawner.TrySpawn(out var secondReturnedPlayer);

            Assert.That(didSpawnFirst, Is.False);
            Assert.That(didSpawnSecond, Is.False);
            Assert.That(firstReturnedPlayer, Is.SameAs(firstPlayer));
            Assert.That(secondReturnedPlayer, Is.SameAs(firstPlayer));
            Assert.That(spawner.SpawnedPlayer, Is.SameAs(firstPlayer));
            Assert.That(CountSpawnedPlayerRoots(spawner), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Start_WhenShouldSpawnOnStartIsFalse_DoesNotSpawn()
        {
            yield return LoadPlayerSandbox();

            var playerTemplate = new GameObject("DisabledAutoSpawnTemplate");
            var spawnPointObject = new GameObject("DisabledAutoSpawnPoint");
            var spawnPoint =
                spawnPointObject.AddComponent<PlayerSpawnPoint>();
            var spawnerObject = new GameObject("DisabledAutoSpawner");
            spawnerObject.SetActive(false);
            var spawner = spawnerObject.AddComponent<PlayerSpawner>();
            spawner.Configure(playerTemplate, spawnPoint);
            JsonUtility.FromJsonOverwrite(
                "{\"shouldSpawnOnStart\":false}",
                spawner);

            spawnerObject.SetActive(true);
            yield return null;

            Assert.That(spawner.SpawnedPlayer, Is.Null);

            Object.Destroy(playerTemplate);
            Object.Destroy(spawnPointObject);
            Object.Destroy(spawnerObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrySpawn_AfterPlayerIsDestroyed_SpawnsReplacement()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var firstPlayer = spawner.SpawnedPlayer;

            Object.Destroy(firstPlayer);
            yield return null;

            var didSpawn = spawner.TrySpawn(out var replacementPlayer);

            Assert.That(didSpawn, Is.True);
            Assert.That(replacementPlayer, Is.Not.Null);
            Assert.That(replacementPlayer, Is.Not.SameAs(firstPlayer));
            Assert.That(spawner.SpawnedPlayer, Is.SameAs(replacementPlayer));
            Assert.That(CountSpawnedPlayerRoots(spawner), Is.EqualTo(1));
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
            return spawners[0];
        }

        private static int CountSpawnedPlayerRoots(PlayerSpawner spawner)
        {
            var expectedName = spawner.PlayerPrefab.name + "(Clone)";
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            var matchingRootCount = 0;

            foreach (var rootObject in rootObjects)
            {
                if (rootObject.name == expectedName)
                {
                    matchingRootCount++;
                }
            }

            return matchingRootCount;
        }
    }
}
