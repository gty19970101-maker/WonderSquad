using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Level.SleepingForest.Greybox;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;
using WonderSquad.Presentation.Camera;
using WonderSquad.Puzzle.SleepingForest;
using WonderSquad.SleepingForest.Completion;
using WonderSquad.UI.Interaction;
using WonderSquad.UI.SleepingForest;

namespace WonderSquad.Tests.EditMode
{
    public sealed class SleepingForestGreyboxEditModeTests
    {
        private const float PlatformSurfaceY = 0f;
        private const float RouteSurfaceY = 0f;
        private const string SleepingForestScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";

        private static readonly string[] AuditedGeometryNames =
        {
            "SpawnAreaGround",
            "ForestEntranceGround",
            "ObservationAreaGround",
            "MainRoute_ToBeaconA",
            "BeaconA_Reserved_Base",
            "MainRoute_BeaconAToNorthwestJunction",
            "MainRouteJunction_Northwest",
            "MainRoute_NorthwestJunctionToBeaconB",
            "BeaconB_Reserved_Base",
            "MainRoute_BeaconBToBridge",
            "RootBridgeEntranceJunction",
            "RootBridgeTemporaryCrossing",
            "RootBridgeMainRouteOuterLeg",
            "RootBridgeMainRouteReturn",
            "SliceEndGround",
            "RecoveryRouteEntryJunction",
            "RecoveryRoute_OuterLoop",
            "RecoveryLeafPad",
            "RecoveryRoute_ReturnNorth",
            "RecoveryRouteReturnJunction",
            "RecoveryRoute_ReturnLoop",
            "AdvantageRouteEntryPlatform",
            "AdvantageRouteSupport",
            "AdvantageRouteHighPlatform",
            "AdvantageRouteVisibleBlocker",
        };

        [Test]
        public void SleepingForestScene_ExistsAtFormalGameplayPath()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(SleepingForestScenePath),
                Is.Not.Null);
        }

        [Test]
        public void SleepingForestScene_HasValidFoundationConfiguration()
        {
            OpenSleepingForest();
            try
            {
                var spawners = Object.FindObjectsByType<PlayerSpawner>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var spawnPoints = Object.FindObjectsByType<PlayerSpawnPoint>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var binders = Object.FindObjectsByType<CameraTargetBinder>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var presenters = Object.FindObjectsByType<InteractionPromptPresenter>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                Assert.That(spawners, Has.Length.EqualTo(1));
                Assert.That(spawnPoints, Has.Length.EqualTo(1));
                Assert.That(binders, Has.Length.EqualTo(1));
                Assert.That(presenters, Has.Length.EqualTo(1));
                Assert.That(spawners[0].IsConfigurationValid, Is.True);
                Assert.That(binders[0].HasValidConfiguration, Is.True);
                Assert.That(presenters[0].HasValidConfiguration, Is.True);
            }
            finally
            {
                ReleaseScene();
            }
        }

        [Test]
        public void SleepingForestScene_HasAllGreyboxAreasAndFormalBeaconAnchors()
        {
            OpenSleepingForest();
            try
            {
                Assert.That(FindSceneObject("ForestEntranceGround"), Is.Not.Null);
                Assert.That(FindSceneObject("ObservationAreaGround"), Is.Not.Null);
                Assert.That(FindSceneObject("ForestBeacon_A"), Is.Not.Null);
                Assert.That(FindSceneObject("ForestBeacon_B"), Is.Not.Null);
                Assert.That(FindSceneObject("MainRoute"), Is.Not.Null);
                Assert.That(FindSceneObject("AdvantageRouteReserved"), Is.Not.Null);
                Assert.That(FindSceneObject("RootBridgeLandmark_LeftRoot"), Is.Null);
                Assert.That(FindSceneObject("RootBridgeLandmark_RightRoot"), Is.Null);
                Assert.That(FindSceneObject("RecoveryRoute"), Is.Not.Null);
                Assert.That(FindSceneObject("SliceEndGround"), Is.Not.Null);
                Assert.That(
                    FindChild(FindSceneObject("ForestBeacon_A"), "DetectionAnchor"),
                    Is.Not.Null);
                Assert.That(
                    FindChild(FindSceneObject("ForestBeacon_B"), "DetectionAnchor"),
                    Is.Not.Null);
            }
            finally
            {
                ReleaseScene();
            }
        }

        [Test]
        public void SleepingForestScene_HasMainRouteCriticalGeometry()
        {
            OpenSleepingForest();
            try
            {
                AssertWalkableRoutePart(
                    "MainRoute_ToBeaconA",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "MainRoute_BeaconAToNorthwestJunction",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "MainRoute_NorthwestJunctionToBeaconB",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "MainRoute_BeaconBToBridge",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "RecoveryRoute_OuterLoop",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "RecoveryRoute_ReturnNorth",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "RecoveryRoute_ReturnLoop",
                    6f,
                    RouteSurfaceY);
                AssertWalkableRoutePart(
                    "RootBridgeTemporaryCrossing",
                    7f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "RootBridgeMainRouteOuterLeg",
                    6f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "RootBridgeMainRouteReturn",
                    3f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "BeaconA_Reserved_Base",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "BeaconB_Reserved_Base",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "RecoveryLeafPad",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "MainRouteJunction_Northwest",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "RootBridgeEntranceJunction",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "RecoveryRouteEntryJunction",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "RecoveryRouteReturnJunction",
                    8f,
                    PlatformSurfaceY);
                AssertWalkableRoutePart(
                    "SliceEndGround",
                    14f,
                    PlatformSurfaceY);
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
            }
            finally
            {
                ReleaseScene();
            }
        }

        [Test]
        public void SleepingForestScene_AuditedGeometryHasNoPositiveVolumeOverlap()
        {
            OpenSleepingForest();
            try
            {
                var auditedObjects = AuditedGeometryNames
                    .Select(FindSceneObject)
                    .ToArray();

                Assert.That(auditedObjects, Has.None.Null);
                for (var firstIndex = 0;
                     firstIndex < auditedObjects.Length;
                     firstIndex++)
                {
                    var firstObject = auditedObjects[firstIndex];
                    var firstRenderer = firstObject.GetComponent<Renderer>();
                    var firstCollider = firstObject.GetComponent<BoxCollider>();
                    Assert.That(firstRenderer, Is.Not.Null, firstObject.name);
                    Assert.That(firstCollider, Is.Not.Null, firstObject.name);

                    for (var secondIndex = firstIndex + 1;
                         secondIndex < auditedObjects.Length;
                         secondIndex++)
                    {
                        var secondObject = auditedObjects[secondIndex];
                        var secondRenderer = secondObject.GetComponent<Renderer>();
                        var secondCollider = secondObject.GetComponent<BoxCollider>();
                        Assert.That(secondRenderer, Is.Not.Null, secondObject.name);
                        Assert.That(secondCollider, Is.Not.Null, secondObject.name);

                        var pairName = firstObject.name + " / " + secondObject.name;
                        Assert.That(
                            HasPositiveVolumeOverlap(
                                firstRenderer.bounds,
                                secondRenderer.bounds),
                            Is.False,
                            "Renderer overlap: " + pairName);
                        Assert.That(
                            HasPositiveVolumeOverlap(
                                firstCollider.bounds,
                                secondCollider.bounds),
                            Is.False,
                            "Collider overlap: " + pairName);
                    }
                }
            }
            finally
            {
                ReleaseScene();
            }
        }

        [Test]
        public void SleepingForestScene_HasValidLocalFallRecoveryConfiguration()
        {
            OpenSleepingForest();
            try
            {
                var recoveries =
                    Object.FindObjectsByType<SleepingForestFallRecovery>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(recoveries, Has.Length.EqualTo(1));
                Assert.That(recoveries[0].HasValidConfiguration, Is.True);
                Assert.That(recoveries[0].RecoveryPoint, Is.Not.Null);
                Assert.That(
                    recoveries[0].RecoveryPoint.gameObject.scene,
                    Is.EqualTo(SceneManager.GetActiveScene()));
                Assert.That(
                    recoveries[0].FallYThreshold,
                    Is.LessThan(recoveries[0].RecoveryPoint.position.y));
                Assert.That(
                    typeof(SleepingForestFallRecovery).Assembly,
                    Is.Not.SameAs(typeof(PlayerMovement).Assembly));
            }
            finally
            {
                ReleaseScene();
            }
        }

        [Test]
        public void SleepingForestScene_HasOnlyApprovedBeaconInteractionGameplay()
        {
            OpenSleepingForest();
            try
            {
                var names = SceneManager.GetActiveScene()
                    .GetRootGameObjects()
                    .SelectMany(GetHierarchyNames)
                    .ToArray();
                var targets = Object.FindObjectsByType<InteractionTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var executionProbes = Object.FindObjectsByType<InteractionExecutionProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var standardProbes = Object.FindObjectsByType<InteractionProbeBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var completionControllers =
                    Object.FindObjectsByType<SleepingForestCompletionController>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                var sliceEndTriggers =
                    Object.FindObjectsByType<SleepingForestSliceEndTrigger>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                var completionPresenters =
                    Object.FindObjectsByType<SleepingForestCompletionPresenter>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(names, Has.None.StartsWith("P0_"));
                Assert.That(names, Does.Not.Contain("DebugCanvas"));
                Assert.That(names, Does.Not.Contain("InteractionTestTarget"));
                var trials = Object.FindObjectsByType<ForestSignalTrial>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                var beaconInteractions =
                    Object.FindObjectsByType<ForestBeaconInteraction>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

                Assert.That(targets, Has.Length.EqualTo(2));
                Assert.That(
                    targets.Select(target => target.TargetId.Value),
                    Is.EquivalentTo(new[]
                    {
                        "sleeping_forest.beacon.a",
                        "sleeping_forest.beacon.b"
                    }));
                Assert.That(trials, Has.Length.EqualTo(1));
                Assert.That(trials[0].HasValidConfiguration, Is.True);
                Assert.That(beaconInteractions, Has.Length.EqualTo(2));
                Assert.That(executionProbes, Is.Empty);
                Assert.That(standardProbes, Is.Empty);
                Assert.That(completionControllers, Has.Length.EqualTo(1));
                Assert.That(
                    completionControllers[0].HasValidConfiguration,
                    Is.True);
                Assert.That(sliceEndTriggers, Has.Length.EqualTo(1));
                Assert.That(
                    sliceEndTriggers[0].HasValidConfiguration,
                    Is.True);
                Assert.That(completionPresenters, Has.Length.EqualTo(1));
                Assert.That(
                    completionPresenters[0].HasValidConfiguration,
                    Is.True);
                Assert.That(
                    names.Any(name => name.Contains("RootBridgeGameplay")),
                    Is.False);
                Assert.That(
                    names.Any(name =>
                        name.Contains("CompletionGameplay") &&
                        name != "SleepingForestCompletion"),
                    Is.False);
            }
            finally
            {
                ReleaseScene();
            }
        }

        [Test]
        public void SleepingForestScene_IsNotYetAddedToBuildSettings()
        {
            Assert.That(
                EditorBuildSettings.scenes.Any(
                    scene => scene.path == SleepingForestScenePath),
                Is.False);
        }

        private static void OpenSleepingForest()
        {
            EditorSceneManager.OpenScene(
                SleepingForestScenePath,
                OpenSceneMode.Single);
        }

        private static void ReleaseScene()
        {
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
        }

        private static GameObject FindSceneObject(string name)
        {
            return SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(GetHierarchyObjects)
                .FirstOrDefault(gameObject => gameObject.name == name);
        }

        private static GameObject FindChild(
            GameObject root,
            string name)
        {
            return root == null
                ? null
                : root.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(transform => transform.name == name)
                    ?.gameObject;
        }

        private static void AssertWalkableRoutePart(
            string objectName,
            float minimumWidth,
            float expectedSurfaceY)
        {
            var routePart = FindSceneObject(objectName);
            Assert.That(routePart, Is.Not.Null, objectName);
            Assert.That(
                routePart.TryGetComponent<BoxCollider>(out var collider),
                Is.True,
                objectName);
            Assert.That(collider.enabled, Is.True, objectName);
            Assert.That(
                routePart.transform.localScale.x,
                Is.GreaterThanOrEqualTo(minimumWidth),
                objectName);
            Assert.That(
                routePart.transform.position.y +
                routePart.transform.localScale.y * 0.5f,
                Is.EqualTo(expectedSurfaceY).Within(0.001f),
                objectName);
        }

        private static string[] GetHierarchyNames(GameObject root)
        {
            return GetHierarchyObjects(root)
                .Select(gameObject => gameObject.name)
                .ToArray();
        }

        private static GameObject[] GetHierarchyObjects(GameObject root)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Select(transform => transform.gameObject)
                .ToArray();
        }

        private static bool HasPositiveVolumeOverlap(
            Bounds first,
            Bounds second)
        {
            const float boundsEpsilon = 0.0001f;
            var overlap = Vector3.Min(first.max, second.max) -
                          Vector3.Max(first.min, second.min);
            return overlap.x > boundsEpsilon &&
                   overlap.y > boundsEpsilon &&
                   overlap.z > boundsEpsilon;
        }
    }
}
