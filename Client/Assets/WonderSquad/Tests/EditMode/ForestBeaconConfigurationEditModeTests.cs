using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Interaction.Prompt;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.Tests.EditMode
{
    public sealed class ForestBeaconConfigurationEditModeTests
    {
        private const string ScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string DefinitionPath =
            "Assets/WonderSquad/ScriptableObjects/Content/SleepingForest/ForestSignalTrialDefinition.asset";
        private const string PromptPath =
            "Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_ForestBeacon.asset";
        private const string PrefabPath =
            "Assets/WonderSquad/Prefabs/SleepingForest/PF_ForestBeacon.prefab";

        [Test]
        public void DefinitionAndPromptAssets_AreValidAndReadOnlyContent()
        {
            var definition =
                AssetDatabase.LoadAssetAtPath<ForestSignalTrialDefinition>(
                    DefinitionPath);
            var prompt =
                AssetDatabase.LoadAssetAtPath<InteractionPromptDefinition>(
                    PromptPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.IsValid, Is.True);
            Assert.That(
                definition.BeaconA.Slot,
                Is.EqualTo(ForestBeaconSlot.BeaconA));
            Assert.That(
                definition.BeaconB.Slot,
                Is.EqualTo(ForestBeaconSlot.BeaconB));
            Assert.That(
                definition.BeaconA.TargetId,
                Is.Not.EqualTo(definition.BeaconB.TargetId));
            Assert.That(prompt, Is.Not.Null);
            Assert.That(prompt.IsValid, Is.True);
        }

        [Test]
        public void ForestBeaconPrefab_HasRequiredComposition()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.layer, Is.EqualTo(8));
            Assert.That(prefab.GetComponent<InteractionTarget>(), Is.Not.Null);
            Assert.That(
                prefab.GetComponent<InteractionPromptSource>(),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponent<ForestBeaconInteraction>(),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponent<ForestBeaconVisual>(),
                Is.Not.Null);

            var trigger = prefab
                .GetComponentsInChildren<Collider>(true)
                .Single(collider => collider.isTrigger);
            Assert.That(trigger.gameObject.layer, Is.EqualTo(8));
            Assert.That(FindChild(prefab, "DetectionAnchor"), Is.Not.Null);
            Assert.That(FindChild(prefab, "DormantMarker"), Is.Not.Null);
            Assert.That(FindChild(prefab, "ActivatedRing"), Is.Not.Null);
            Assert.That(FindChild(prefab, "IncorrectMarker"), Is.Not.Null);
        }

        [Test]
        public void SleepingForest_HasExactlyTwoConfiguredFormalBeacons()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var trials = Object.FindObjectsByType<ForestSignalTrial>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var interactions =
                    Object.FindObjectsByType<ForestBeaconInteraction>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                var targets = Object.FindObjectsByType<InteractionTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                Assert.That(trials, Has.Length.EqualTo(1));
                Assert.That(trials[0].HasValidConfiguration, Is.True);
                Assert.That(interactions, Has.Length.EqualTo(2));
                Assert.That(targets, Has.Length.EqualTo(2));
                Assert.That(
                    targets.Select(target => target.TargetId.Value),
                    Is.EquivalentTo(new[]
                    {
                        "sleeping_forest.beacon.a",
                        "sleeping_forest.beacon.b"
                    }));
                Assert.That(
                    interactions.Select(interaction => interaction.Slot),
                    Is.EquivalentTo(new[]
                    {
                        ForestBeaconSlot.BeaconA,
                        ForestBeaconSlot.BeaconB
                    }));
                AssertSerializedReference(
                    trials[0],
                    "trialDefinition",
                    trials[0].TrialDefinition);
                AssertSerializedReference(
                    trials[0],
                    "beaconA",
                    trials[0].BeaconA);
                AssertSerializedReference(
                    trials[0],
                    "beaconB",
                    trials[0].BeaconB);

                foreach (var interaction in interactions)
                {
                    AssertSerializedReference(
                        interaction,
                        "trial",
                        trials[0]);
                    AssertSerializedEnum(
                        interaction,
                        "slot",
                        (int)interaction.Slot);
                }

                foreach (var visual in Object.FindObjectsByType<ForestBeaconVisual>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    AssertSerializedReference(
                        visual,
                        "trial",
                        trials[0]);
                    AssertSerializedEnum(
                        visual,
                        "slot",
                        (int)visual.Slot);
                }
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        [Test]
        public void SleepingForest_ContainsNoDiagnosticProbeOrFutureConsequences()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            try
            {
                var hierarchyNames = SceneManager.GetActiveScene()
                    .GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<Transform>(true))
                    .Select(transform => transform.name)
                    .ToArray();

                Assert.That(
                    Object.FindObjectsByType<InteractionExecutionProbe>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Is.Empty);
                Assert.That(
                    Object.FindObjectsByType<InteractionProbeBehaviour>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None),
                    Is.Empty);
                Assert.That(hierarchyNames, Does.Not.Contain("InteractionTestTarget"));
                Assert.That(hierarchyNames, Has.None.StartsWith("P0_"));
                Assert.That(hierarchyNames, Has.None.Contains("RootBridgeGameplay"));
                Assert.That(hierarchyNames, Has.None.Contains("CompletionGameplay"));
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        [Test]
        public void Assemblies_PreserveFoundationDependencyDirection()
        {
            var puzzleAssembly = File.ReadAllText(
                "Assets/WonderSquad/Runtime/Puzzle/WonderSquad.Puzzle.asmdef");
            var interactionAssembly = File.ReadAllText(
                "Assets/WonderSquad/Runtime/Interaction/WonderSquad.Interaction.asmdef");

            Assert.That(puzzleAssembly, Does.Contain("WonderSquad.Core"));
            Assert.That(puzzleAssembly, Does.Contain("WonderSquad.Content"));
            Assert.That(puzzleAssembly, Does.Not.Contain("WonderSquad.Interaction"));
            Assert.That(puzzleAssembly, Does.Not.Contain("WonderSquad.Player"));
            Assert.That(interactionAssembly, Does.Not.Contain("WonderSquad.Puzzle"));
        }

        [Test]
        public void InteractionContracts_RemainUnchangedAndTrialEventIsInstanceScoped()
        {
            Assert.That(
                typeof(IInteractable).GetProperties().Select(property => property.Name),
                Is.EquivalentTo(new[]
                {
                    "TargetId",
                    "DetectionPosition",
                    "IsDetectionEnabled",
                    "DetectionPriority"
                }));
            Assert.That(
                typeof(IExecutableInteraction).GetMethods().Select(method => method.Name),
                Is.EquivalentTo(new[] { "get_TargetId", "get_IsExecutionAvailable", "Execute" }));

            var trialEvent =
                typeof(ForestSignalTrial).GetEvent(
                    nameof(ForestSignalTrial.TrialStateChanged));
            Assert.That(trialEvent, Is.Not.Null);
            Assert.That(trialEvent.AddMethod.IsStatic, Is.False);
        }

        private static GameObject FindChild(
            GameObject root,
            string name)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == name)
                ?.gameObject;
        }

        private static void AssertSerializedReference(
            UnityEngine.Object target,
            string propertyPath,
            UnityEngine.Object expectedValue)
        {
            var property = new SerializedObject(target).FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            Assert.That(
                property.objectReferenceValue,
                Is.SameAs(expectedValue),
                propertyPath);
        }

        private static void AssertSerializedEnum(
            UnityEngine.Object target,
            string propertyPath,
            int expectedValue)
        {
            var property = new SerializedObject(target).FindProperty(propertyPath);
            Assert.That(property, Is.Not.Null, propertyPath);
            Assert.That(property.enumValueIndex, Is.EqualTo(expectedValue), propertyPath);
        }
    }
}
