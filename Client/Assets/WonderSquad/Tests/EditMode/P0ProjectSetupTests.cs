using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WonderSquad.Editor;

namespace WonderSquad.Tests.EditMode
{
    public sealed class P0ProjectSetupTests
    {
        private const string TemporaryRootPath =
            "Assets/WonderSquad/Tests/EditMode/P0ProjectSetupTestTemp";

        private string _testDirectoryPath;
        private string _isolationScenePath;
        private string _testScenePath;

        [SetUp]
        public void SetUp()
        {
            ReleaseTemporaryScenes();
            DeleteTemporaryRoot();

            _testDirectoryPath =
                TemporaryRootPath + "/" + Guid.NewGuid().ToString("N");
            _isolationScenePath =
                _testDirectoryPath + "/IsolationScene.unity";
            _testScenePath = _testDirectoryPath + "/TestScene.unity";
            EnsureAssetFolder(_testDirectoryPath);
            CreateIsolationScene();
        }

        [TearDown]
        public void TearDown()
        {
            ReleaseTemporaryScenes();
            DeleteTemporaryRoot();
        }

        [Test]
        public void EnsureP0SceneExists_WhenMissing_CreatesScene()
        {
            var created = P0ProjectSetup.EnsureP0SceneExists(
                _testScenePath,
                "GeneratedRoot");

            Assert.That(created, Is.True);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(_testScenePath),
                Is.Not.Null);
        }

        [Test]
        public void EnsureP0SceneExists_WhenPresent_DoesNotOverwriteScene()
        {
            CreateSceneWithSentinel();
            var originalContents = ReadSceneContents();

            var created = P0ProjectSetup.EnsureP0SceneExists(
                _testScenePath,
                "GeneratedRoot");

            Assert.That(created, Is.False);
            Assert.That(ReadSceneContents(), Is.EqualTo(originalContents));
        }

        [Test]
        public void EnsureP0SceneExists_WhenRepeated_DoesNotChangeExistingScene()
        {
            Assert.That(
                P0ProjectSetup.EnsureP0SceneExists(
                    _testScenePath,
                    "GeneratedRoot"),
                Is.True);
            var originalContents = ReadSceneContents();

            Assert.That(
                P0ProjectSetup.EnsureP0SceneExists(
                    _testScenePath,
                    "GeneratedRoot"),
                Is.False);
            Assert.That(ReadSceneContents(), Is.EqualTo(originalContents));
        }

        [Test]
        public void AutomaticSetup_UsesCreateMissingOnlyMode()
        {
            Assert.That(
                P0ProjectSetup.AutomaticSceneCreationMode,
                Is.EqualTo(P0SceneCreationMode.CreateMissingOnly));
        }

        private void CreateSceneWithSentinel()
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            var sentinel = new GameObject("ExistingSceneSentinel");
            SceneManager.MoveGameObjectToScene(sentinel, scene);

            Assert.That(
                EditorSceneManager.SaveScene(scene, _testScenePath),
                Is.True);
            Assert.That(
                EditorSceneManager.CloseScene(scene, true),
                Is.True);
        }

        private void CreateIsolationScene()
        {
            var isolationScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Assert.That(
                EditorSceneManager.SaveScene(
                    isolationScene,
                    _isolationScenePath),
                Is.True);
        }

        private string ReadSceneContents()
        {
            return File.ReadAllText(_testScenePath);
        }

        private static void ReleaseTemporaryScenes()
        {
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Assert.That(
                SceneManager.sceneCount,
                Is.EqualTo(1),
                "The test must own an isolated scene lifecycle.");
            Assert.That(
                SceneManager.GetSceneAt(0).path,
                Is.Empty,
                "Temporary test scenes must be released before cleanup.");
        }

        private static void DeleteTemporaryRoot()
        {
            if (!AssetDatabase.IsValidFolder(TemporaryRootPath))
            {
                return;
            }

            Assert.That(
                AssetDatabase.DeleteAsset(TemporaryRootPath),
                Is.True,
                $"Failed to delete temporary test directory: {TemporaryRootPath}");
            AssetDatabase.Refresh();
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var currentPath = "Assets";
            var pathParts = assetPath.Split('/');

            for (var index = 1; index < pathParts.Length; index++)
            {
                var nextPath = currentPath + "/" + pathParts[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
