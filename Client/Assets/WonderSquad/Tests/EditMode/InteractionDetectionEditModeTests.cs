using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Interaction.Detection;

namespace WonderSquad.Tests.EditMode
{
    public sealed class InteractionDetectionEditModeTests
    {
        private const string InteractionSettingsPath =
            "Assets/WonderSquad/ScriptableObjects/Configuration/InteractionSettings.asset";
        private const string InteractionAssemblyPath =
            "Assets/WonderSquad/Runtime/Interaction/WonderSquad.Interaction.asmdef";
        private const string PlayerAssemblyPath =
            "Assets/WonderSquad/Runtime/Player/WonderSquad.Player.asmdef";
        private const string InteractableLayerName = "Interactable";

        [Test]
        public void IInteractable_ContractIsReadOnlyAndHasNoExecutionMethod()
        {
            var contractType = typeof(IInteractable);

            Assert.That(contractType.IsInterface, Is.True);
            Assert.That(
                contractType.GetProperty(nameof(IInteractable.TargetId)),
                Is.Not.Null);
            Assert.That(
                contractType.GetProperty(
                    nameof(IInteractable.DetectionPosition)),
                Is.Not.Null);
            Assert.That(
                contractType.GetProperty(
                    nameof(IInteractable.IsDetectionEnabled)),
                Is.Not.Null);
            Assert.That(contractType.GetMethod("Interact"), Is.Null);

            foreach (var property in contractType.GetProperties())
            {
                Assert.That(property.CanRead, Is.True);
                Assert.That(property.CanWrite, Is.False);
            }
        }

        [Test]
        public void InteractionSettings_DefaultValuesAndAssetAreValid()
        {
            var runtimeDefaults =
                ScriptableObject.CreateInstance<InteractionSettings>();
            var settingsAsset =
                AssetDatabase.LoadAssetAtPath<InteractionSettings>(
                    InteractionSettingsPath);

            try
            {
                Assert.That(runtimeDefaults.IsValid, Is.True);
                Assert.That(settingsAsset, Is.Not.Null);
                Assert.That(settingsAsset.IsValid, Is.True);
                Assert.That(settingsAsset.DetectionRadius, Is.GreaterThan(0f));
                Assert.That(settingsAsset.MaxCandidateCount, Is.GreaterThan(0));

                var interactionLayer =
                    LayerMask.NameToLayer(InteractableLayerName);
                Assert.That(interactionLayer, Is.GreaterThanOrEqualTo(0));
                Assert.That(
                    settingsAsset.InteractionLayerMask.value,
                    Is.EqualTo(1 << interactionLayer));
            }
            finally
            {
                Object.DestroyImmediate(runtimeDefaults);
            }
        }

        [Test]
        public void InteractionSettings_InvalidParametersAreRejected()
        {
            var settings =
                ScriptableObject.CreateInstance<InteractionSettings>();
            var serializedSettings = new SerializedObject(settings);

            try
            {
                SetFloat(
                    serializedSettings,
                    "detectionRadius",
                    0f);
                Assert.That(settings.IsValid, Is.False);

                SetFloat(
                    serializedSettings,
                    "detectionRadius",
                    2f);
                SetInteger(
                    serializedSettings,
                    "interactionLayerMask",
                    0);
                Assert.That(settings.IsValid, Is.False);

                SetInteger(
                    serializedSettings,
                    "interactionLayerMask",
                    ~0);
                SetInteger(
                    serializedSettings,
                    "maxCandidateCount",
                    0);
                Assert.That(settings.IsValid, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void InteractionDetector_RequiresOriginAndValidSettings()
        {
            var player = new GameObject("InteractionDetectorTestPlayer");
            var origin = new GameObject("DetectionOrigin");
            var settings =
                ScriptableObject.CreateInstance<InteractionSettings>();

            player.SetActive(false);
            origin.transform.SetParent(player.transform, false);
            var detector = player.AddComponent<InteractionDetector>();

            try
            {
                Assert.That(detector.HasValidConfiguration, Is.False);
                Assert.That(
                    detector.Configure(origin.transform, settings),
                    Is.True);
                Assert.That(detector.HasValidConfiguration, Is.True);
                Assert.That(
                    detector.CandidateBufferCapacity,
                    Is.EqualTo(settings.MaxCandidateCount));
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void InteractionValidator_AcceptsValidDistanceLayerAndVisibility()
        {
            var settings = CreateSettingsForLayer(8);
            var candidate =
                new FakeInteractable(
                    "interaction.test.valid",
                    Vector3.forward,
                    true);
            var validator = new InteractionValidator();

            try
            {
                var isValid = validator.TryValidate(
                    candidate,
                    8,
                    Vector3.zero,
                    settings,
                    false,
                    out var squaredDistance);

                Assert.That(isValid, Is.True);
                Assert.That(squaredDistance, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void InteractionValidator_RejectsDistanceLayerAndOcclusion()
        {
            var settings = CreateSettingsForLayer(8);
            var validator = new InteractionValidator();

            try
            {
                var farCandidate =
                    new FakeInteractable(
                        "interaction.test.far",
                        Vector3.forward * 10f,
                        true);
                Assert.That(
                    validator.TryValidate(
                        farCandidate,
                        8,
                        Vector3.zero,
                        settings,
                        false,
                        out _),
                    Is.False);

                var nearCandidate =
                    new FakeInteractable(
                        "interaction.test.near",
                        Vector3.forward,
                        true);
                Assert.That(
                    validator.TryValidate(
                        nearCandidate,
                        0,
                        Vector3.zero,
                        settings,
                        false,
                        out _),
                    Is.False);
                Assert.That(
                    validator.TryValidate(
                        nearCandidate,
                        8,
                        Vector3.zero,
                        settings,
                        true,
                        out _),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void InteractionAssembly_DependenciesRemainOneWayThroughCore()
        {
            var interactionAssembly =
                LoadAssemblyDefinition(InteractionAssemblyPath);
            var playerAssembly =
                LoadAssemblyDefinition(PlayerAssemblyPath);

            Assert.That(
                interactionAssembly.references,
                Is.EqualTo(new[] { "WonderSquad.Core" }));
            Assert.That(
                playerAssembly.references,
                Does.Not.Contain("WonderSquad.Interaction"));
        }

        private static InteractionSettings CreateSettingsForLayer(int layer)
        {
            var settings =
                ScriptableObject.CreateInstance<InteractionSettings>();
            var serializedSettings = new SerializedObject(settings);
            SetInteger(
                serializedSettings,
                "interactionLayerMask",
                1 << layer);
            return settings;
        }

        private static void SetFloat(
            SerializedObject serializedObject,
            string propertyName,
            float value)
        {
            serializedObject.FindProperty(propertyName).floatValue =
                value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(
            SerializedObject serializedObject,
            string propertyName,
            int value)
        {
            serializedObject.FindProperty(propertyName).intValue =
                value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AssemblyDefinitionData LoadAssemblyDefinition(
            string path)
        {
            var json = File.ReadAllText(path);
            var definition =
                JsonUtility.FromJson<AssemblyDefinitionData>(json);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.references, Is.Not.Null);
            return definition;
        }

        [System.Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string[] references;
        }

        private sealed class FakeInteractable : IInteractable
        {
            public FakeInteractable(
                string targetId,
                Vector3 position,
                bool isDetectionEnabled)
            {
                TargetId = new InteractionTargetId(targetId);
                DetectionPosition = position;
                IsDetectionEnabled = isDetectionEnabled;
            }

            public InteractionTargetId TargetId { get; }

            public Vector3 DetectionPosition { get; }

            public bool IsDetectionEnabled { get; }

            public int DetectionPriority => 0;
        }
    }
}
