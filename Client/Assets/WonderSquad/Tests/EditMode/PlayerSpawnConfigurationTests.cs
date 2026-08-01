using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using WonderSquad.Core.Configuration;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Tests.EditMode
{
    public sealed class PlayerSpawnConfigurationTests
    {
        private const string PlayerPrefabPath =
            "Assets/WonderSquad/Prefabs/Player/Player.prefab";

        [Test]
        public void PlayerPrefab_IsAValidPrefabAsset()
        {
            var playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Assert.That(playerPrefab, Is.Not.Null);
            Assert.That(
                PrefabUtility.IsPartOfPrefabAsset(playerPrefab),
                Is.True);
            Assert.That(playerPrefab.name, Is.EqualTo("Player"));
        }

        [Test]
        public void PlayerSandbox_HasValidSpawnConfiguration()
        {
            EditorSceneManager.OpenScene(
                ProjectConstants.PlayerSandboxScenePath,
                OpenSceneMode.Single);

            try
            {
                var spawners = Object.FindObjectsByType<PlayerSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var spawnPoints = Object.FindObjectsByType<PlayerSpawnPoint>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                Assert.That(spawners, Has.Length.EqualTo(1));
                Assert.That(spawnPoints, Has.Length.EqualTo(1));
                Assert.That(spawners[0].IsConfigurationValid, Is.True);
                Assert.That(
                    spawners[0].PlayerPrefab,
                    Is.EqualTo(
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            PlayerPrefabPath)));
                Assert.That(spawners[0].SpawnPoint, Is.SameAs(spawnPoints[0]));
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        [Test]
        public void ShouldSpawnOnStart_HasLegacySerializationMigration()
        {
            var currentField = typeof(PlayerSpawner).GetField(
                "shouldSpawnOnStart",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var legacyField = typeof(PlayerSpawner).GetField(
                "spawnOnStart",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(currentField, Is.Not.Null);
            Assert.That(legacyField, Is.Null);
            Assert.That(
                currentField.IsDefined(typeof(SerializeField), false),
                Is.True);

            var migrationAttribute =
                currentField.GetCustomAttribute<FormerlySerializedAsAttribute>();

            Assert.That(migrationAttribute, Is.Not.Null);
            Assert.That(
                migrationAttribute.oldName,
                Is.EqualTo("spawnOnStart"));
        }
    }
}
