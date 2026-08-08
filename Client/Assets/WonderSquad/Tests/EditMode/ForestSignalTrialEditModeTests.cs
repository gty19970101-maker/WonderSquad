using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Execution;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.Tests.EditMode
{
    public sealed class ForestSignalTrialEditModeTests
    {
        private TrialFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new TrialFixture();
        }

        [TearDown]
        public void TearDown()
        {
            fixture?.Dispose();
        }

        [Test]
        public void InitialSnapshot_IsDormantAndAwaitingBeaconA()
        {
            var snapshot = fixture.Trial.CurrentSnapshot;

            Assert.That(fixture.Definition.IsValid, Is.True);
            Assert.That(fixture.Trial.HasValidConfiguration, Is.True);
            Assert.That(
                snapshot.Phase,
                Is.EqualTo(ForestSignalTrialPhase.AwaitingFirstBeacon));
            Assert.That(
                snapshot.BeaconA.LogicalState,
                Is.EqualTo(ForestBeaconLogicalState.Dormant));
            Assert.That(
                snapshot.BeaconB.LogicalState,
                Is.EqualTo(ForestBeaconLogicalState.Dormant));
            Assert.That(snapshot.LastOutcome, Is.EqualTo(ForestSignalTrialOutcome.None));
            Assert.That(snapshot.Revision, Is.Zero);
            Assert.That(snapshot.IsCompleted, Is.False);
        }

        [Test]
        public void ExecuteAThenB_CompletesWithDeterministicRevisions()
        {
            Assert.That(
                fixture.Execute(fixture.BeaconA, 1U).Code,
                Is.EqualTo(InteractionResultCode.Success));
            var afterA = fixture.Trial.CurrentSnapshot;
            Assert.That(afterA.BeaconA.IsActivated, Is.True);
            Assert.That(afterA.BeaconB.IsActivated, Is.False);
            Assert.That(
                afterA.Phase,
                Is.EqualTo(ForestSignalTrialPhase.AwaitingSecondBeacon));
            Assert.That(afterA.Revision, Is.EqualTo(1U));

            Assert.That(
                fixture.Execute(fixture.BeaconB, 2U).Code,
                Is.EqualTo(InteractionResultCode.Success));
            var completed = fixture.Trial.CurrentSnapshot;
            Assert.That(completed.BeaconA.IsActivated, Is.True);
            Assert.That(completed.BeaconB.IsActivated, Is.True);
            Assert.That(completed.IsCompleted, Is.True);
            Assert.That(
                completed.LastOutcome,
                Is.EqualTo(ForestSignalTrialOutcome.TrialCompleted));
            Assert.That(completed.Revision, Is.EqualTo(2U));
        }

        [Test]
        public void ExecuteBBeforeA_RecordsIncorrectOrderWithoutActivation()
        {
            var result = fixture.Execute(fixture.BeaconB, 1U);
            var snapshot = fixture.Trial.CurrentSnapshot;

            Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Success));
            Assert.That(snapshot.BeaconA.IsActivated, Is.False);
            Assert.That(snapshot.BeaconB.IsActivated, Is.False);
            Assert.That(snapshot.HasIncorrectOrder, Is.True);
            Assert.That(
                snapshot.LastAttemptedSlot,
                Is.EqualTo(ForestBeaconSlot.BeaconB));
            Assert.That(snapshot.Revision, Is.EqualTo(1U));
            Assert.That(fixture.BeaconB.IsExecutionAvailable, Is.True);
        }

        [Test]
        public void IncorrectOrder_RecoversThroughAThenB()
        {
            fixture.Execute(fixture.BeaconB, 1U);
            fixture.Execute(fixture.BeaconA, 2U);

            var afterA = fixture.Trial.CurrentSnapshot;
            Assert.That(afterA.HasIncorrectOrder, Is.False);
            Assert.That(afterA.BeaconA.IsActivated, Is.True);
            Assert.That(afterA.BeaconB.IsActivated, Is.False);
            Assert.That(
                fixture.VisualB.CurrentMode,
                Is.EqualTo(ForestBeaconVisualMode.Dormant));

            fixture.Execute(fixture.BeaconB, 3U);
            Assert.That(fixture.Trial.CurrentSnapshot.IsCompleted, Is.True);
            Assert.That(fixture.Trial.CurrentSnapshot.Revision, Is.EqualTo(3U));
        }

        [Test]
        public void ActivatedBeacon_DirectRepeatReturnsBusyWithoutProgress()
        {
            fixture.Execute(fixture.BeaconA, 1U);
            var beforeRepeat = fixture.Trial.CurrentSnapshot;

            var result = fixture.Execute(fixture.BeaconA, 2U);
            var afterRepeat = fixture.Trial.CurrentSnapshot;

            Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Busy));
            Assert.That(afterRepeat.Revision, Is.EqualTo(beforeRepeat.Revision));
            Assert.That(afterRepeat.BeaconA.IsActivated, Is.True);
            Assert.That(
                afterRepeat.BeaconA.LogicalState,
                Is.Not.EqualTo(ForestBeaconLogicalState.Dormant));
        }

        [Test]
        public void CompletedTrial_RejectsRepeatsAndPublishesCompletionOnce()
        {
            var eventCount = 0;
            fixture.Trial.TrialStateChanged += _ => eventCount++;

            fixture.Execute(fixture.BeaconA, 1U);
            fixture.Execute(fixture.BeaconB, 2U);
            var completedRevision = fixture.Trial.CurrentSnapshot.Revision;
            var repeatA = fixture.Execute(fixture.BeaconA, 3U);
            var repeatB = fixture.Execute(fixture.BeaconB, 4U);

            Assert.That(eventCount, Is.EqualTo(2));
            Assert.That(repeatA.Code, Is.EqualTo(InteractionResultCode.Busy));
            Assert.That(repeatB.Code, Is.EqualTo(InteractionResultCode.Busy));
            Assert.That(
                fixture.Trial.CurrentSnapshot.Revision,
                Is.EqualTo(completedRevision));
            Assert.That(fixture.Trial.CurrentSnapshot.IsCompleted, Is.True);
        }

        [Test]
        public void UnregisteredBeacon_ReturnsTargetInvalidWithoutStateChange()
        {
            var unknown = fixture.CreateUnregisteredBeacon();
            var before = fixture.Trial.CurrentSnapshot;

            var result = fixture.Execute(unknown, 1U);

            Assert.That(
                result.Code,
                Is.EqualTo(InteractionResultCode.TargetInvalid));
            Assert.That(
                fixture.Trial.CurrentSnapshot.Revision,
                Is.EqualTo(before.Revision));
            Assert.That(fixture.Trial.CurrentSnapshot.IsCompleted, Is.False);
        }

        [Test]
        public void DuplicateRequestThroughPort_ReturnsCachedResultWithoutProgress()
        {
            var context = fixture.CreateContext(1U);
            var request = new InteractionRequest(
                context,
                fixture.BeaconA.TargetId);

            var first = fixture.RequestPort.Execute(request, fixture.BeaconA);
            var replay = fixture.RequestPort.Execute(request, fixture.BeaconA);

            Assert.That(first.Code, Is.EqualTo(InteractionResultCode.Success));
            Assert.That(replay.Code, Is.EqualTo(InteractionResultCode.Success));
            Assert.That(fixture.Trial.CurrentSnapshot.Revision, Is.EqualTo(1U));
        }

        [Test]
        public void Snapshot_IsReadOnlyAndContainsSprint003CBoundary()
        {
            var properties = typeof(ForestSignalTrialSnapshot).GetProperties();

            Assert.That(
                properties.Where(property => property.CanWrite),
                Is.Empty);
            Assert.That(
                properties.Select(property => property.Name),
                Does.Contain(nameof(ForestSignalTrialSnapshot.BeaconA)));
            Assert.That(
                properties.Select(property => property.Name),
                Does.Contain(nameof(ForestSignalTrialSnapshot.BeaconB)));
            Assert.That(
                properties.Select(property => property.Name),
                Does.Contain(nameof(ForestSignalTrialSnapshot.HasIncorrectOrder)));
            Assert.That(
                properties.Select(property => property.Name),
                Does.Contain(nameof(ForestSignalTrialSnapshot.IsCompleted)));
            Assert.That(
                properties.Select(property => property.Name),
                Does.Contain(nameof(ForestSignalTrialSnapshot.Revision)));
        }

        [Test]
        public void RuntimeExecution_DoesNotMutateDefinition()
        {
            var stableId = fixture.Definition.StableId;
            var schemaVersion = fixture.Definition.SchemaVersion;
            var beaconATargetId = fixture.Definition.BeaconA.TargetId;
            var beaconBTargetId = fixture.Definition.BeaconB.TargetId;

            fixture.Execute(fixture.BeaconB, 1U);
            fixture.Execute(fixture.BeaconA, 2U);
            fixture.Execute(fixture.BeaconB, 3U);

            Assert.That(fixture.Definition.StableId, Is.EqualTo(stableId));
            Assert.That(fixture.Definition.SchemaVersion, Is.EqualTo(schemaVersion));
            Assert.That(
                fixture.Definition.BeaconA.TargetId,
                Is.EqualTo(beaconATargetId));
            Assert.That(
                fixture.Definition.BeaconB.TargetId,
                Is.EqualTo(beaconBTargetId));
        }

        [Test]
        public void Visuals_UseIndependentPropertyBlocksAndMarkers()
        {
            var originalSharedColor = fixture.SharedMaterial.GetColor("_BaseColor");

            fixture.Execute(fixture.BeaconB, 1U);

            Assert.That(
                fixture.VisualA.CurrentMode,
                Is.EqualTo(ForestBeaconVisualMode.Dormant));
            Assert.That(
                fixture.VisualB.CurrentMode,
                Is.EqualTo(ForestBeaconVisualMode.Incorrect));
            Assert.That(fixture.IncorrectMarkerA.activeSelf, Is.False);
            Assert.That(fixture.IncorrectMarkerB.activeSelf, Is.True);

            var blockA = new MaterialPropertyBlock();
            var blockB = new MaterialPropertyBlock();
            fixture.RendererA.GetPropertyBlock(blockA);
            fixture.RendererB.GetPropertyBlock(blockB);
            Assert.That(
                blockA.GetColor("_BaseColor"),
                Is.Not.EqualTo(blockB.GetColor("_BaseColor")));
            Assert.That(
                fixture.SharedMaterial.GetColor("_BaseColor"),
                Is.EqualTo(originalSharedColor));
            Assert.That(
                fixture.RendererA.sharedMaterial,
                Is.SameAs(fixture.RendererB.sharedMaterial));
        }

        [Test]
        public void BeaconTargetIds_AreStableDistinctAndMatchDefinition()
        {
            Assert.That(fixture.BeaconA.TargetId.IsValid, Is.True);
            Assert.That(fixture.BeaconB.TargetId.IsValid, Is.True);
            Assert.That(fixture.BeaconA.TargetId, Is.Not.EqualTo(fixture.BeaconB.TargetId));
            Assert.That(
                fixture.BeaconA.TargetId,
                Is.EqualTo(fixture.Definition.BeaconA.TargetId));
            Assert.That(
                fixture.BeaconB.TargetId,
                Is.EqualTo(fixture.Definition.BeaconB.TargetId));
        }

        private sealed class TrialFixture : IDisposable
        {
            private const string BeaconATargetId =
                "sleeping_forest.beacon.a";
            private const string BeaconBTargetId =
                "sleeping_forest.beacon.b";

            private readonly GameObject root;

            public TrialFixture()
            {
                root = new GameObject("ForestSignalTrialEditModeFixture");
                root.SetActive(false);
                Definition =
                    ScriptableObject.CreateInstance<ForestSignalTrialDefinition>();
                Definition.Configure(
                    "sleeping_forest.forest_signal_trial",
                    1,
                    BeaconATargetId,
                    BeaconBTargetId);

                SharedMaterial = CreateMaterial();
                var trialObject = CreateChild("Trial");
                Trial = trialObject.AddComponent<ForestSignalTrial>();
                BeaconA = CreateBeacon(
                    "BeaconA",
                    BeaconATargetId,
                    ForestBeaconSlot.BeaconA,
                    out var visualA,
                    out var rendererA,
                    out var dormantMarkerA,
                    out var activatedMarkerA,
                    out var incorrectMarkerA);
                BeaconB = CreateBeacon(
                    "BeaconB",
                    BeaconBTargetId,
                    ForestBeaconSlot.BeaconB,
                    out var visualB,
                    out var rendererB,
                    out var dormantMarkerB,
                    out var activatedMarkerB,
                    out var incorrectMarkerB);
                VisualA = visualA;
                VisualB = visualB;
                RendererA = rendererA;
                RendererB = rendererB;
                DormantMarkerA = dormantMarkerA;
                DormantMarkerB = dormantMarkerB;
                ActivatedMarkerA = activatedMarkerA;
                ActivatedMarkerB = activatedMarkerB;
                IncorrectMarkerA = incorrectMarkerA;
                IncorrectMarkerB = incorrectMarkerB;
                RequestPort =
                    CreateChild("RequestPort")
                        .AddComponent<LocalInteractionRequestPort>();

                Assert.That(
                    Trial.Configure(Definition, BeaconA, BeaconB),
                    Is.True);
                root.SetActive(true);
                ConfigureVisual(
                    VisualA,
                    ForestBeaconSlot.BeaconA,
                    RendererA,
                    DormantMarkerA,
                    ActivatedMarkerA,
                    IncorrectMarkerA);
                ConfigureVisual(
                    VisualB,
                    ForestBeaconSlot.BeaconB,
                    RendererB,
                    DormantMarkerB,
                    ActivatedMarkerB,
                    IncorrectMarkerB);
            }

            public ForestSignalTrialDefinition Definition { get; }

            public ForestSignalTrial Trial { get; }

            public ForestBeaconInteraction BeaconA { get; }

            public ForestBeaconInteraction BeaconB { get; }

            public ForestBeaconVisual VisualA { get; }

            public ForestBeaconVisual VisualB { get; }

            public Renderer RendererA { get; }

            public Renderer RendererB { get; }

            public GameObject DormantMarkerA { get; }

            public GameObject DormantMarkerB { get; }

            public GameObject ActivatedMarkerA { get; }

            public GameObject ActivatedMarkerB { get; }

            public GameObject IncorrectMarkerA { get; }

            public GameObject IncorrectMarkerB { get; }

            public Material SharedMaterial { get; }

            public LocalInteractionRequestPort RequestPort { get; }

            public InteractionResult Execute(
                ForestBeaconInteraction beacon,
                uint requestId)
            {
                return beacon.Execute(CreateContext(requestId));
            }

            public InteractionContext CreateContext(uint requestId)
            {
                return new InteractionContext(
                    new PlayerId(1UL),
                    new RequestId(requestId),
                    requestId,
                    Vector3.zero,
                    Vector3.forward);
            }

            public ForestBeaconInteraction CreateUnregisteredBeacon()
            {
                return CreateBeacon(
                    "UnknownBeacon",
                    "sleeping_forest.beacon.unknown",
                    ForestBeaconSlot.BeaconA,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _);
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(Definition);
                UnityEngine.Object.DestroyImmediate(SharedMaterial);
            }

            private ForestBeaconInteraction CreateBeacon(
                string name,
                string targetId,
                ForestBeaconSlot slot,
                out ForestBeaconVisual visual,
                out Renderer renderer,
                out GameObject dormantMarker,
                out GameObject activatedMarker,
                out GameObject incorrectMarker)
            {
                var beaconObject = CreateChild(name);
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(beaconObject.transform, false);
                renderer = body.GetComponent<Renderer>();
                renderer.sharedMaterial = SharedMaterial;

                dormantMarker = CreateMarker(beaconObject, "Dormant");
                activatedMarker = CreateMarker(beaconObject, "Activated");
                incorrectMarker = CreateMarker(beaconObject, "Incorrect");
                var target = beaconObject.AddComponent<InteractionTarget>();
                target.Configure(targetId, beaconObject.transform, true, 0);
                var interaction =
                    beaconObject.AddComponent<ForestBeaconInteraction>();
                interaction.Configure(target, Trial, slot);
                visual = beaconObject.AddComponent<ForestBeaconVisual>();
                return interaction;
            }

            private void ConfigureVisual(
                ForestBeaconVisual visual,
                ForestBeaconSlot slot,
                Renderer renderer,
                GameObject dormantMarker,
                GameObject activatedMarker,
                GameObject incorrectMarker)
            {
                Assert.That(
                    visual.Configure(
                        Trial,
                        slot,
                        renderer,
                        dormantMarker,
                        activatedMarker,
                        incorrectMarker,
                        Color.blue,
                        Color.green,
                        Color.red),
                    Is.True);
            }

            private GameObject CreateChild(string name)
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                return child;
            }

            private static GameObject CreateMarker(
                GameObject parent,
                string name)
            {
                var marker = new GameObject(name);
                marker.transform.SetParent(parent.transform, false);
                return marker;
            }

            private static Material CreateMaterial()
            {
                var shader =
                    Shader.Find("Universal Render Pipeline/Lit") ??
                    Shader.Find("Standard");
                var material = new Material(shader);
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", Color.white);
                }

                return material;
            }
        }
    }
}
