using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Interaction.Prompt;

namespace WonderSquad.Tests.EditMode
{
    public sealed class InteractionProbeEditModeTests
    {
        private static readonly int BaseColorPropertyId =
            Shader.PropertyToID("_BaseColor");

        private const string ProbePrefabPath =
            "Assets/WonderSquad/Prefabs/Interaction/PF_InteractionProbe.prefab";
        private const string ProbeBehaviourSourcePath =
            "Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeBehaviour.cs";
        private const string ProbeVisualStateSourcePath =
            "Assets/WonderSquad/Runtime/Interaction/Diagnostics/InteractionProbeVisualState.cs";

        [Test]
        public void NewProbe_StartsInactiveWithMatchingVisualState()
        {
            var fixture = CreateProbeFixture();
            try
            {
                Assert.That(fixture.Probe.IsActive, Is.False);
                Assert.That(fixture.Probe.ExecutionCount, Is.EqualTo(0));
                Assert.That(fixture.VisualState.IsDisplayingActive, Is.False);
                Assert.That(fixture.Probe.IsExecutionAvailable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void Execute_WhenContextIsValid_TogglesActiveAndReturnsSuccess()
        {
            var fixture = CreateProbeFixture();
            try
            {
                var context = CreateContext(new RequestId(10U));
                var result = fixture.Probe.Execute(context);

                Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Success));
                Assert.That(result.RequestId, Is.EqualTo(context.RequestId));
                Assert.That(result.TargetId, Is.EqualTo(fixture.Target.TargetId));
                Assert.That(fixture.Probe.IsActive, Is.True);
                Assert.That(fixture.Probe.ExecutionCount, Is.EqualTo(1));
                Assert.That(fixture.Probe.LastRequestId, Is.EqualTo(context.RequestId));
                Assert.That(fixture.VisualState.IsDisplayingActive, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void Execute_TwiceWithValidContexts_RestoresInactiveState()
        {
            var fixture = CreateProbeFixture();
            try
            {
                fixture.Probe.Execute(CreateContext(new RequestId(11U)));
                var result = fixture.Probe.Execute(
                    CreateContext(new RequestId(12U)));

                Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Success));
                Assert.That(fixture.Probe.IsActive, Is.False);
                Assert.That(fixture.Probe.ExecutionCount, Is.EqualTo(2));
                Assert.That(
                    fixture.Probe.LastRequestId,
                    Is.EqualTo(new RequestId(12U)));
                Assert.That(fixture.VisualState.IsDisplayingActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void Execute_WhenContextIsInvalid_ReturnsUnknownWithoutChangingState()
        {
            var fixture = CreateProbeFixture();
            try
            {
                var result = fixture.Probe.Execute(default);

                Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Unknown));
                Assert.That(fixture.Probe.IsActive, Is.False);
                Assert.That(fixture.Probe.ExecutionCount, Is.EqualTo(0));
                Assert.That(fixture.Probe.LastRequestId.IsValid, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(fixture.Root);
            }
        }

        [Test]
        public void ProbeVisualState_TwoInstancesKeepIndependentPropertyBlocks()
        {
            var firstFixture = CreateProbeFixture();
            var secondFixture = CreateProbeFixture();
            try
            {
                firstFixture.Probe.Execute(CreateContext(new RequestId(13U)));

                Assert.That(firstFixture.Probe.IsActive, Is.True);
                Assert.That(secondFixture.Probe.IsActive, Is.False);
                Assert.That(
                    GetDisplayedBaseColor(firstFixture.Root),
                    Is.EqualTo(Color.green));
                Assert.That(
                    GetDisplayedBaseColor(secondFixture.Root),
                    Is.EqualTo(Color.white));
            }
            finally
            {
                Object.DestroyImmediate(firstFixture.Root);
                Object.DestroyImmediate(secondFixture.Root);
            }
        }

        [Test]
        public void ProbePrefab_HasCompleteComposedInteractionConfiguration()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProbePrefabPath);

            Assert.That(prefab, Is.Not.Null);
            var target = prefab.GetComponent<InteractionTarget>();
            var promptSource = prefab.GetComponent<InteractionPromptSource>();
            var visualState = prefab.GetComponent<InteractionProbeVisualState>();
            var probe = prefab.GetComponent<InteractionProbeBehaviour>();
            var collider = prefab.GetComponent<Collider>();

            Assert.That(target, Is.Not.Null);
            Assert.That(promptSource, Is.Not.Null);
            Assert.That(visualState, Is.Not.Null);
            Assert.That(probe, Is.Not.Null);
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.isTrigger, Is.True);
            Assert.That(target.IsConfigurationValid, Is.True);
            Assert.That(promptSource.HasValidPromptConfiguration, Is.True);
            Assert.That(visualState.HasValidConfiguration, Is.True);
            Assert.That(probe.HasValidConfiguration, Is.True);
            Assert.That(probe.TargetId, Is.EqualTo(target.TargetId));
            Assert.That(prefab.layer, Is.EqualTo(LayerMask.NameToLayer("Interactable")));
        }

        [Test]
        public void CoreInteractionContracts_RetainApprovedSignatures()
        {
            Assert.That(
                typeof(IInteractable).GetProperty(nameof(IInteractable.TargetId)),
                Is.Not.Null);
            Assert.That(
                typeof(IInteractable).GetProperty(nameof(IInteractable.DetectionPosition)),
                Is.Not.Null);
            Assert.That(
                typeof(IExecutableInteraction).GetProperty(
                    nameof(IExecutableInteraction.IsExecutionAvailable)),
                Is.Not.Null);
            Assert.That(
                typeof(IExecutableInteraction).GetMethod(
                    nameof(IExecutableInteraction.Execute)),
                Is.Not.Null);
            Assert.That(typeof(InteractionContext).IsValueType, Is.True);
            Assert.That(typeof(InteractionRequest).IsValueType, Is.True);
            Assert.That(typeof(InteractionResult).IsValueType, Is.True);
        }

        [Test]
        public void ProbeRuntime_DoesNotReferenceForbiddenDomainsOrGlobalLookup()
        {
            var behaviourSource = ReadProjectSource(ProbeBehaviourSourcePath);
            var visualSource = ReadProjectSource(ProbeVisualStateSourcePath);
            var combinedSource = behaviourSource + visualSource;

            Assert.That(combinedSource, Does.Not.Contain("FindObject"));
            Assert.That(combinedSource, Does.Not.Contain("ServiceLocator"));
            Assert.That(combinedSource, Does.Not.Contain("static Interaction"));
            Assert.That(combinedSource, Does.Not.Contain("WonderSquad.Player"));
            Assert.That(combinedSource, Does.Not.Contain("WonderSquad.UI"));
            Assert.That(combinedSource, Does.Not.Contain("WonderSquad.Puzzle"));
            Assert.That(combinedSource, Does.Not.Contain("WonderSquad.Inventory"));
            Assert.That(combinedSource, Does.Not.Contain("WonderSquad.Network"));
            Assert.That(combinedSource, Does.Not.Contain("Animator"));
            Assert.That(combinedSource, Does.Not.Contain("AudioSource"));
            Assert.That(combinedSource, Does.Not.Contain(".material"));
        }

        private static InteractionProbeFixture CreateProbeFixture()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.layer = LayerMask.NameToLayer("Interactable");

            var target = root.AddComponent<InteractionTarget>();
            Assert.That(
                target.Configure(
                    "interaction.probe.edit_mode",
                    root.transform,
                    true,
                    0),
                Is.True);

            var visualState = root.AddComponent<InteractionProbeVisualState>();
            Assert.That(
                visualState.Configure(
                    root.GetComponent<Renderer>(),
                    Color.white,
                    Color.green),
                Is.True);

            var probe = root.AddComponent<InteractionProbeBehaviour>();
            Assert.That(
                probe.Configure(target, visualState, true),
                Is.True);

            return new InteractionProbeFixture(root, target, visualState, probe);
        }

        private static InteractionContext CreateContext(RequestId requestId)
        {
            return new InteractionContext(
                new PlayerId(1UL),
                requestId,
                1d,
                Vector3.zero,
                Vector3.forward);
        }

        private static Color GetDisplayedBaseColor(GameObject root)
        {
            var propertyBlock = new MaterialPropertyBlock();
            root.GetComponent<Renderer>().GetPropertyBlock(propertyBlock);
            return propertyBlock.GetColor(BaseColorPropertyId);
        }

        private static string ReadProjectSource(string relativePath)
        {
            return File.ReadAllText(
                Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        }

        private readonly struct InteractionProbeFixture
        {
            public InteractionProbeFixture(
                GameObject root,
                InteractionTarget target,
                InteractionProbeVisualState visualState,
                InteractionProbeBehaviour probe)
            {
                Root = root;
                Target = target;
                VisualState = visualState;
                Probe = probe;
            }

            public GameObject Root { get; }

            public InteractionTarget Target { get; }

            public InteractionProbeVisualState VisualState { get; }

            public InteractionProbeBehaviour Probe { get; }
        }
    }
}
