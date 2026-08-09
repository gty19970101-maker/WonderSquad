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
using WonderSquad.Presentation.Camera;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class SleepingForestRootBridgeConsequencePlayModeTests
    {
        private const string SleepingForestScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string CleanupSceneName =
            "SleepingForestRootBridgeConsequenceCleanup";

        [UnityTest]
        public IEnumerator SleepingForest_InitialStateKeepsMainRouteAndSpanInactive()
        {
            yield return LoadSleepingForest();

            var consequence = Object.FindFirstObjectByType<
                SleepingForestRootBridgeConsequence>();
            var mainBridge = FindSceneObject("RootBridgeTemporaryCrossing");
            var recovery = Object.FindFirstObjectByType<
                SleepingForestFallRecovery>();

            Assert.That(consequence, Is.Not.Null);
            Assert.That(consequence.HasValidConfiguration, Is.True);
            Assert.That(
                consequence.CurrentState,
                Is.EqualTo(SleepingForestRootBridgeState.Initial));
            Assert.That(consequence.AdvantageSpan.GroundRenderer.enabled, Is.False);
            Assert.That(consequence.AdvantageSpan.GroundCollider.enabled, Is.False);
            Assert.That(mainBridge, Is.Not.Null);
            Assert.That(mainBridge.GetComponent<Renderer>().enabled, Is.True);
            Assert.That(mainBridge.GetComponent<BoxCollider>().enabled, Is.True);
            Assert.That(recovery, Is.Not.Null);
            Assert.That(recovery.HasValidConfiguration, Is.True);
        }

        [UnityTest]
        public IEnumerator SleepingForest_CompletedTrialActivatesSpanWithoutChangingMainBridge()
        {
            yield return LoadSleepingForest();

            var consequence = Object.FindFirstObjectByType<
                SleepingForestRootBridgeConsequence>();
            var trial = Object.FindFirstObjectByType<ForestSignalTrial>();
            var mainBridge = FindSceneObject("RootBridgeTemporaryCrossing");
            var mainPosition = mainBridge.transform.position;
            var mainRendererEnabled = mainBridge.GetComponent<Renderer>().enabled;
            var mainColliderEnabled = mainBridge.GetComponent<BoxCollider>().enabled;

            Execute(trial.BeaconA, 1U);
            Execute(trial.BeaconB, 2U);
            yield return null;

            Assert.That(trial.CurrentSnapshot.IsCompleted, Is.True);
            Assert.That(
                consequence.CurrentState,
                Is.EqualTo(SleepingForestRootBridgeState.Activated));
            Assert.That(consequence.AdvantageSpan.GroundRenderer.enabled, Is.True);
            Assert.That(consequence.AdvantageSpan.GroundCollider.enabled, Is.True);
            Assert.That(mainBridge.transform.position, Is.EqualTo(mainPosition));
            Assert.That(
                mainBridge.GetComponent<Renderer>().enabled,
                Is.EqualTo(mainRendererEnabled));
            Assert.That(
                mainBridge.GetComponent<BoxCollider>().enabled,
                Is.EqualTo(mainColliderEnabled));
        }

        [UnityTest]
        public IEnumerator SleepingForest_CompletedStateKeepsPlayerCameraAndRecoveryStable()
        {
            yield return LoadSleepingForest();

            var consequence = Object.FindFirstObjectByType<
                SleepingForestRootBridgeConsequence>();
            var trial = Object.FindFirstObjectByType<ForestSignalTrial>();
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            var binder = Object.FindFirstObjectByType<CameraTargetBinder>();
            var recovery = Object.FindFirstObjectByType<
                SleepingForestFallRecovery>();

            Execute(trial.BeaconA, 1U);
            Execute(trial.BeaconB, 2U);
            yield return null;

            Assert.That(consequence.AdvantageSpan.GroundCollider.enabled, Is.True);
            Assert.That(player, Is.Not.Null);
            Assert.That(player.isActiveAndEnabled, Is.True);
            Assert.That(player.GetComponent<CharacterController>().enabled, Is.True);
            Assert.That(binder, Is.Not.Null);
            Assert.That(binder.IsBound, Is.True);
            Assert.That(recovery, Is.Not.Null);
            Assert.That(recovery.HasValidConfiguration, Is.True);
        }

        [UnityTest]
        public IEnumerator SleepingForest_ReenableConsequenceAtSameRevisionKeepsSingleActiveSpan()
        {
            yield return LoadSleepingForest();

            var consequence = Object.FindFirstObjectByType<
                SleepingForestRootBridgeConsequence>();
            var trial = Object.FindFirstObjectByType<ForestSignalTrial>();
            Execute(trial.BeaconA, 1U);
            Execute(trial.BeaconB, 2U);
            yield return null;
            var revision = consequence.LastAppliedRevision;
            var span = consequence.AdvantageSpan;

            consequence.gameObject.SetActive(false);
            yield return null;
            consequence.gameObject.SetActive(true);
            yield return null;

            Assert.That(consequence.LastAppliedRevision, Is.EqualTo(revision));
            Assert.That(consequence.AdvantageSpan, Is.SameAs(span));
            Assert.That(span.GroundCollider.enabled, Is.True);
            Assert.That(
                Object.FindObjectsByType<RootBridgeAdvantageSpan>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
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

            yield return null;
        }

        private static void Execute(ForestBeaconInteraction beacon, uint requestId)
        {
            Assert.That(beacon, Is.Not.Null);
            var result = beacon.Execute(new InteractionContext(
                new PlayerId(1UL),
                new RequestId(requestId),
                requestId,
                Vector3.zero,
                Vector3.forward));
            Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Success));
        }

        private static GameObject FindSceneObject(string name)
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }

            return null;
        }

        [UnityTearDown]
        public IEnumerator UnloadSleepingForestAfterTest()
        {
            var sleepingForest = SceneManager.GetSceneByPath(SleepingForestScenePath);
            if (!sleepingForest.IsValid() || !sleepingForest.isLoaded)
            {
                yield break;
            }

            var cleanupScene = SceneManager.CreateScene(CleanupSceneName);
            SceneManager.SetActiveScene(cleanupScene);
            var operation = SceneManager.UnloadSceneAsync(sleepingForest);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }
        }
    }
}
