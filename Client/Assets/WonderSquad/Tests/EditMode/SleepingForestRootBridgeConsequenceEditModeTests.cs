using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Interaction.Detection;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.Tests.EditMode
{
    public sealed class SleepingForestRootBridgeConsequenceEditModeTests
    {
        private const string SleepingForestScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const float MaximumAdvantageRouteRatio = 0.65f;
        private const float MinimumConnectionWidth = 1.5f;
        private const float MinimumParallelSeparation = 10f;

        private BridgeFixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new BridgeFixture();
        }

        [TearDown]
        public void TearDown()
        {
            fixture.Dispose();
        }

        [Test]
        public void InitialSnapshot_KeepsAdvantageSpanInactive()
        {
            Assert.That(
                fixture.Consequence.CurrentState,
                Is.EqualTo(SleepingForestRootBridgeState.Initial));
            Assert.That(fixture.Span.GroundRenderer.enabled, Is.False);
            Assert.That(fixture.Span.GroundCollider.enabled, Is.False);
            Assert.That(fixture.DormantMarker.activeSelf, Is.True);
            Assert.That(fixture.ActivatedMarker.activeSelf, Is.False);
        }

        [Test]
        public void CompletedTrial_ActivatesOnlyIndependentAdvantageSpan()
        {
            var mainPosition = fixture.MainBridge.transform.position;
            var mainRendererEnabled = fixture.MainBridgeRenderer.enabled;
            var mainColliderEnabled = fixture.MainBridgeCollider.enabled;

            fixture.CompleteTrial();

            Assert.That(
                fixture.Consequence.CurrentState,
                Is.EqualTo(SleepingForestRootBridgeState.Activated));
            Assert.That(fixture.Span.GroundRenderer.enabled, Is.True);
            Assert.That(fixture.Span.GroundCollider.enabled, Is.True);
            Assert.That(fixture.DormantMarker.activeSelf, Is.False);
            Assert.That(fixture.ActivatedMarker.activeSelf, Is.True);
            Assert.That(
                fixture.MainBridge.transform.position,
                Is.EqualTo(mainPosition));
            Assert.That(fixture.MainBridgeRenderer.enabled, Is.EqualTo(mainRendererEnabled));
            Assert.That(fixture.MainBridgeCollider.enabled, Is.EqualTo(mainColliderEnabled));
        }

        [Test]
        public void IncorrectOrder_DoesNotActivateShortcutAndCanRecover()
        {
            fixture.Execute(fixture.BeaconB, 1U);

            Assert.That(fixture.Trial.CurrentSnapshot.HasIncorrectOrder, Is.True);
            Assert.That(fixture.Span.GroundCollider.enabled, Is.False);
            Assert.That(
                fixture.Consequence.CurrentState,
                Is.EqualTo(SleepingForestRootBridgeState.Initial));

            fixture.Execute(fixture.BeaconA, 2U);
            fixture.Execute(fixture.BeaconB, 3U);

            Assert.That(fixture.Trial.CurrentSnapshot.IsCompleted, Is.True);
            Assert.That(fixture.Span.GroundCollider.enabled, Is.True);
        }

        [Test]
        public void ReenableAtSameCompletedRevision_DoesNotCreateOrChangeAnotherSpan()
        {
            fixture.CompleteTrial();
            var completedRevision = fixture.Consequence.LastAppliedRevision;
            var span = fixture.Span;

            fixture.Consequence.gameObject.SetActive(false);
            fixture.Consequence.gameObject.SetActive(true);

            Assert.That(fixture.Consequence.LastAppliedRevision, Is.EqualTo(completedRevision));
            Assert.That(fixture.Consequence.AdvantageSpan, Is.SameAs(span));
            Assert.That(fixture.Span.GroundCollider.enabled, Is.True);
            Assert.That(fixture.Span.GroundRenderer.enabled, Is.True);
        }

        [Test]
        public void Visual_UsesPropertyBlockWithoutChangingSharedMaterial()
        {
            var originalColor = fixture.SharedMaterial.GetColor("_BaseColor");
            var initialBlock = new MaterialPropertyBlock();
            fixture.Span.GroundRenderer.GetPropertyBlock(initialBlock);

            fixture.CompleteTrial();
            var activatedBlock = new MaterialPropertyBlock();
            fixture.Span.GroundRenderer.GetPropertyBlock(activatedBlock);

            Assert.That(
                initialBlock.GetColor("_BaseColor"),
                Is.Not.EqualTo(activatedBlock.GetColor("_BaseColor")));
            Assert.That(
                fixture.SharedMaterial.GetColor("_BaseColor"),
                Is.EqualTo(originalColor));
            Assert.That(
                fixture.Span.GroundRenderer.sharedMaterial,
                Is.SameAs(fixture.SharedMaterial));
        }

        [Test]
        public void TwoSpanVisuals_KeepIndependentPropertyBlockStates()
        {
            var secondaryRoot = new GameObject("SecondarySpanVisual");
            try
            {
                var secondaryGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
                secondaryGround.transform.SetParent(secondaryRoot.transform, false);
                var secondaryRenderer = secondaryGround.GetComponent<Renderer>();
                secondaryRenderer.sharedMaterial = fixture.SharedMaterial;
                var secondaryDormantMarker = new GameObject("SecondaryDormant");
                secondaryDormantMarker.transform.SetParent(secondaryRoot.transform, false);
                var secondaryActivatedMarker = new GameObject("SecondaryActivated");
                secondaryActivatedMarker.transform.SetParent(secondaryRoot.transform, false);
                var secondaryVisual = secondaryRoot.AddComponent<RootBridgeAdvantageVisual>();
                Assert.That(
                    secondaryVisual.Configure(
                        secondaryRenderer,
                        secondaryDormantMarker,
                        secondaryActivatedMarker,
                        Color.red,
                        Color.green),
                    Is.True);
                secondaryVisual.InitializeState();

                fixture.CompleteTrial();

                var primaryBlock = new MaterialPropertyBlock();
                var secondaryBlock = new MaterialPropertyBlock();
                fixture.Span.GroundRenderer.GetPropertyBlock(primaryBlock);
                secondaryRenderer.GetPropertyBlock(secondaryBlock);
                Assert.That(
                    primaryBlock.GetColor("_BaseColor"),
                    Is.Not.EqualTo(secondaryBlock.GetColor("_BaseColor")));
                Assert.That(secondaryDormantMarker.activeSelf, Is.True);
                Assert.That(secondaryActivatedMarker.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(secondaryRoot);
            }
        }

        [Test]
        public void SpanGeometry_HasNoPositiveVolumeOverlapWithMainBridge()
        {
            Assert.That(
                HasPositiveVolumeOverlap(
                    fixture.Span.GroundRenderer.bounds,
                    fixture.MainBridgeRenderer.bounds),
                Is.False);
            Assert.That(
                HasPositiveVolumeOverlap(
                    GetConfiguredBounds(fixture.Span.GroundCollider),
                    GetConfiguredBounds(fixture.MainBridgeCollider)),
                Is.False);
        }

        [Test]
        public void SleepingForestScene_HasValidInitialRootBridgeConsequenceComposition()
        {
            EditorSceneManager.OpenScene(
                SleepingForestScenePath,
                OpenSceneMode.Single);
            try
            {
                var consequences = UnityEngine.Object.FindObjectsByType<
                    SleepingForestRootBridgeConsequence>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var spans = UnityEngine.Object.FindObjectsByType<RootBridgeAdvantageSpan>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var mainBridge = FindSceneObject("RootBridgeTemporaryCrossing");

                Assert.That(consequences, Has.Length.EqualTo(1));
                Assert.That(spans, Has.Length.EqualTo(1));
                Assert.That(consequences[0].HasValidConfiguration, Is.True);
                Assert.That(mainBridge, Is.Not.Null);
                Assert.That(mainBridge.GetComponent<Renderer>().enabled, Is.True);
                Assert.That(mainBridge.GetComponent<BoxCollider>().enabled, Is.True);
                Assert.That(spans[0].GroundRenderer.enabled, Is.False);
                Assert.That(spans[0].GroundCollider.enabled, Is.False);
                Assert.That(
                    FindSceneObject("Boundary_RootBridgeWestGuardrail"),
                    Is.Null);
                Assert.That(
                    FindSceneObject("Boundary_RootBridgeEastGuardrail"),
                    Is.Null);
                Assert.That(
                    FindSceneObject("RootBridgeLandmark_LeftRoot"),
                    Is.Null);
                Assert.That(
                    FindSceneObject("RootBridgeLandmark_RightRoot"),
                    Is.Null);
                Assert.That(
                    HasPositiveVolumeOverlap(
                        GetConfiguredBounds(spans[0].GroundCollider),
                        GetConfiguredBounds(mainBridge.GetComponent<BoxCollider>())),
                    Is.False);
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        [Test]
        public void SleepingForestScene_AdvantageRouteIsIndependentAndShorter()
        {
            EditorSceneManager.OpenScene(
                SleepingForestScenePath,
                OpenSceneMode.Single);
            try
            {
                var entrance = FindSceneObject("RootBridgeEntranceJunction");
                var mainBridge = FindSceneObject("RootBridgeTemporaryCrossing");
                var outerLeg = FindSceneObject("RootBridgeMainRouteOuterLeg");
                var returnLeg = FindSceneObject("RootBridgeMainRouteReturn");
                var sliceEnd = FindSceneObject("SliceEndGround");
                var span = UnityEngine.Object.FindFirstObjectByType<
                    RootBridgeAdvantageSpan>(FindObjectsInactive.Include);

                Assert.That(entrance, Is.Not.Null);
                Assert.That(mainBridge, Is.Not.Null);
                Assert.That(outerLeg, Is.Not.Null);
                Assert.That(returnLeg, Is.Not.Null);
                Assert.That(sliceEnd, Is.Not.Null);
                Assert.That(span, Is.Not.Null);

                var mainBounds = GetConfiguredBounds(
                    mainBridge.GetComponent<BoxCollider>());
                var outerBounds = GetConfiguredBounds(
                    outerLeg.GetComponent<BoxCollider>());
                var returnBounds = GetConfiguredBounds(
                    returnLeg.GetComponent<BoxCollider>());
                var advantageBounds = GetConfiguredBounds(span.GroundCollider);
                var entranceBounds = GetConfiguredBounds(
                    entrance.GetComponent<BoxCollider>());
                var endBounds = GetConfiguredBounds(
                    sliceEnd.GetComponent<BoxCollider>());

                AssertNoPositiveVolumeOverlap(
                    entranceBounds,
                    mainBounds,
                    outerBounds,
                    returnBounds,
                    advantageBounds,
                    endBounds);
                AssertWalkableConnection(
                    entranceBounds,
                    mainBounds);
                AssertWalkableConnection(
                    mainBounds,
                    outerBounds);
                AssertWalkableConnection(outerBounds, returnBounds);
                AssertWalkableConnection(returnBounds, endBounds);
                AssertWalkableConnection(
                    entranceBounds,
                    advantageBounds);
                AssertWalkableConnection(advantageBounds, endBounds);

                Assert.That(mainBounds.size.x, Is.GreaterThan(mainBounds.size.z));
                Assert.That(
                    advantageBounds.size.z,
                    Is.GreaterThan(advantageBounds.size.x));
                Assert.That(
                    outerBounds.min.x - advantageBounds.max.x,
                    Is.GreaterThanOrEqualTo(MinimumParallelSeparation));

                var mainLength = CalculatePolylineLength(
                    entrance.transform.position,
                    mainBridge.transform.position,
                    outerLeg.transform.position,
                    returnLeg.transform.position,
                    sliceEnd.transform.position);
                var advantageLength = CalculatePolylineLength(
                    entrance.transform.position,
                    span.transform.position,
                    sliceEnd.transform.position);
                Assert.That(
                    advantageLength,
                    Is.LessThanOrEqualTo(mainLength * MaximumAdvantageRouteRatio));
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        [Test]
        public void SleepingForestScene_AdvantageCorridorHasNoObsoleteLargeCube()
        {
            EditorSceneManager.OpenScene(
                SleepingForestScenePath,
                OpenSceneMode.Single);
            try
            {
                var span = UnityEngine.Object.FindFirstObjectByType<
                    RootBridgeAdvantageSpan>(FindObjectsInactive.Include);
                Assert.That(span, Is.Not.Null);
                Assert.That(
                    FindSceneObject("RootBridgeLandmark_LeftRoot"),
                    Is.Null);
                Assert.That(
                    FindSceneObject("RootBridgeLandmark_RightRoot"),
                    Is.Null);

                var cleanCorridor = new Bounds(
                    new Vector3(0f, 1.5f, 35f),
                    new Vector3(14f, 6f, 16f));
                var approvedGround = span.GroundCollider.gameObject;
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        var gameObject = transform.gameObject;
                        if (gameObject == approvedGround ||
                            !gameObject.TryGetComponent<MeshRenderer>(out _) ||
                            !gameObject.TryGetComponent<MeshFilter>(out _) ||
                            !gameObject.TryGetComponent<BoxCollider>(out var collider))
                        {
                            continue;
                        }

                        Assert.That(
                            HasPositiveVolumeOverlap(
                                cleanCorridor,
                                GetConfiguredBounds(collider)),
                            Is.False,
                            $"Unapproved visible/collidable geometry in " +
                            $"Advantage corridor: {GetHierarchyPath(transform)}");
                    }
                }
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        private static bool HasPositiveVolumeOverlap(Bounds first, Bounds second)
        {
            return first.min.x < second.max.x && first.max.x > second.min.x &&
                   first.min.y < second.max.y && first.max.y > second.min.y &&
                   first.min.z < second.max.z && first.max.z > second.min.z;
        }

        private static void AssertWalkableConnection(Bounds from, Bounds to)
        {
            const float edgeTolerance = 0.001f;
            var overlapX = Mathf.Min(from.max.x, to.max.x) -
                           Mathf.Max(from.min.x, to.min.x);
            var overlapZ = Mathf.Min(from.max.z, to.max.z) -
                           Mathf.Max(from.min.z, to.min.z);
            var touchesOnX =
                Mathf.Abs(from.max.x - to.min.x) <= edgeTolerance ||
                Mathf.Abs(to.max.x - from.min.x) <= edgeTolerance;
            var touchesOnZ =
                Mathf.Abs(from.max.z - to.min.z) <= edgeTolerance ||
                Mathf.Abs(to.max.z - from.min.z) <= edgeTolerance;
            Assert.That(
                touchesOnX && overlapZ >= MinimumConnectionWidth ||
                touchesOnZ && overlapX >= MinimumConnectionWidth,
                Is.True);
        }

        private static void AssertNoPositiveVolumeOverlap(params Bounds[] bounds)
        {
            for (var firstIndex = 0; firstIndex < bounds.Length; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1;
                     secondIndex < bounds.Length;
                     secondIndex++)
                {
                    Assert.That(
                        HasPositiveVolumeOverlap(
                            bounds[firstIndex],
                            bounds[secondIndex]),
                        Is.False);
                }
            }
        }

        private static Bounds GetConfiguredBounds(Collider collider)
        {
            var boxCollider = collider as BoxCollider;
            Assert.That(boxCollider, Is.Not.Null);
            var scale = boxCollider.transform.lossyScale;
            var absoluteScale = new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));
            return new Bounds(
                boxCollider.transform.TransformPoint(boxCollider.center),
                Vector3.Scale(boxCollider.size, absoluteScale));
        }

        private static float CalculatePolylineLength(params Vector3[] points)
        {
            var length = 0f;
            for (var index = 1; index < points.Length; index++)
            {
                length += Vector3.Distance(points[index - 1], points[index]);
            }

            return length;
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

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        private sealed class BridgeFixture : IDisposable
        {
            private const string BeaconATargetId = "sleeping_forest.beacon.a";
            private const string BeaconBTargetId = "sleeping_forest.beacon.b";

            private readonly GameObject root;

            public BridgeFixture()
            {
                root = new GameObject("SleepingForestRootBridgeFixture");
                root.SetActive(false);
                SharedMaterial = CreateMaterial();
                Definition = ScriptableObject.CreateInstance<ForestSignalTrialDefinition>();
                Definition.Configure(
                    "sleeping_forest.forest_signal_trial",
                    1,
                    BeaconATargetId,
                    BeaconBTargetId);

                Trial = CreateChild("ForestSignalTrial")
                    .AddComponent<ForestSignalTrial>();
                BeaconA = CreateBeacon(
                    "BeaconA",
                    BeaconATargetId,
                    ForestBeaconSlot.BeaconA);
                BeaconB = CreateBeacon(
                    "BeaconB",
                    BeaconBTargetId,
                    ForestBeaconSlot.BeaconB);
                Assert.That(Trial.Configure(Definition, BeaconA, BeaconB), Is.True);

                MainBridge = CreateGround(
                    "RootBridgeTemporaryCrossing",
                    new Vector3(8.5f, -0.5f, 23f),
                    new Vector3(9f, 1f, 6f));
                MainBridgeRenderer = MainBridge.GetComponent<Renderer>();
                MainBridgeCollider = MainBridge.GetComponent<BoxCollider>();

                var spanObject = CreateChild("RootBridgeAdvantageSpan");
                var spanGround = CreateGround(
                    "GroundSurface",
                    new Vector3(0f, -0.5f, 35f),
                    new Vector3(3f, 1f, 16f));
                spanGround.transform.SetParent(spanObject.transform, true);
                DormantMarker = CreateMarker(spanObject, "DormantMarker");
                ActivatedMarker = CreateMarker(spanObject, "ActivatedMarker");
                Span = spanObject.AddComponent<RootBridgeAdvantageSpan>();
                Visual = spanObject.AddComponent<RootBridgeAdvantageVisual>();
                Assert.That(
                    Span.Configure(
                        spanGround.GetComponent<Renderer>(),
                        spanGround.GetComponent<BoxCollider>()),
                    Is.True);
                Assert.That(
                    Visual.Configure(
                        spanGround.GetComponent<Renderer>(),
                        DormantMarker,
                        ActivatedMarker,
                        Color.red,
                        Color.green),
                    Is.True);

                var consequenceObject = CreateChild("ForestSignalRouteConsequence");
                Consequence = consequenceObject.AddComponent<SleepingForestRootBridgeConsequence>();
                root.SetActive(true);
                Assert.That(Consequence.Configure(Trial, Span, Visual), Is.True);
            }

            public ForestSignalTrialDefinition Definition { get; }

            public ForestSignalTrial Trial { get; }

            public ForestBeaconInteraction BeaconA { get; }

            public ForestBeaconInteraction BeaconB { get; }

            public SleepingForestRootBridgeConsequence Consequence { get; }

            public RootBridgeAdvantageSpan Span { get; }

            public RootBridgeAdvantageVisual Visual { get; }

            public GameObject DormantMarker { get; }

            public GameObject ActivatedMarker { get; }

            public GameObject MainBridge { get; }

            public Renderer MainBridgeRenderer { get; }

            public BoxCollider MainBridgeCollider { get; }

            public Material SharedMaterial { get; }

            public void CompleteTrial()
            {
                Execute(BeaconA, 1U);
                Execute(BeaconB, 2U);
            }

            public void Execute(ForestBeaconInteraction beacon, uint requestId)
            {
                var result = beacon.Execute(new InteractionContext(
                    new PlayerId(1UL),
                    new RequestId(requestId),
                    requestId,
                    Vector3.zero,
                    Vector3.forward));
                Assert.That(result.Code, Is.EqualTo(InteractionResultCode.Success));
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
                ForestBeaconSlot slot)
            {
                var beacon = CreateChild(name);
                var target = beacon.AddComponent<InteractionTarget>();
                target.Configure(targetId, beacon.transform, true, 0);
                var interaction = beacon.AddComponent<ForestBeaconInteraction>();
                interaction.Configure(target, Trial, slot);
                return interaction;
            }

            private GameObject CreateGround(
                string name,
                Vector3 position,
                Vector3 scale)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = name;
                ground.transform.SetParent(root.transform, false);
                ground.transform.position = position;
                ground.transform.localScale = scale;
                ground.GetComponent<Renderer>().sharedMaterial = SharedMaterial;
                return ground;
            }

            private GameObject CreateChild(string name)
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                return child;
            }

            private static GameObject CreateMarker(GameObject parent, string name)
            {
                var marker = new GameObject(name);
                marker.transform.SetParent(parent.transform, false);
                return marker;
            }

            private static Material CreateMaterial()
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ??
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
