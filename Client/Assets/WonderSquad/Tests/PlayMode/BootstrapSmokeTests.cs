using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Bootstrap;
using WonderSquad.Core.Configuration;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class BootstrapSmokeTests
    {
        [UnityTest]
        public IEnumerator RuntimeInitialization_CreatesOneBootstrapRoot()
        {
            yield return null;

            Assert.That(BootstrapEntry.IsInitialized, Is.True);
            Assert.That(
                GameObject.Find(ProjectConstants.BootstrapRootName),
                Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator P0SandboxScenes_CanRunIndependently()
        {
            var scenePaths = new[]
            {
                ProjectConstants.PlayerSandboxScenePath,
                ProjectConstants.GameplaySandboxScenePath,
                ProjectConstants.RecoverySandboxScenePath
            };

            foreach (var scenePath in scenePaths)
            {
                var loadOperation = SceneManager.LoadSceneAsync(
                    scenePath,
                    LoadSceneMode.Single);

                Assert.That(loadOperation, Is.Not.Null);
                while (!loadOperation.isDone)
                {
                    yield return null;
                }

                Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scenePath));
                Assert.That(BootstrapEntry.IsInitialized, Is.True);
                Assert.That(Camera.main, Is.Not.Null);
                Assert.That(GameObject.Find("Ground_Placeholder"), Is.Not.Null);
                Assert.That(GameObject.Find("CameraMount"), Is.Not.Null);
                Assert.That(GameObject.Find("SpawnPoint"), Is.Not.Null);
                Assert.That(GameObject.Find("BoundaryRoot"), Is.Not.Null);
                Assert.That(GameObject.Find("DebugCanvas"), Is.Not.Null);
                Assert.That(
                    GameObject.Find(ProjectConstants.BootstrapRootName),
                    Is.Not.Null);
            }
        }
    }
}
