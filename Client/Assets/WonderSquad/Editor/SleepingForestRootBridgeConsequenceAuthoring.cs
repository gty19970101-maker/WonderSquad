using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.Editor
{
    /// <summary>
    /// Applies the explicitly approved Sprint003C Root Bridge consequence.
    /// It is intentionally menu-only and never runs during project startup.
    /// </summary>
    public static class SleepingForestRootBridgeConsequenceAuthoring
    {
        private const string MenuPath =
            "Wonder Squad/Sprint003C/Apply Root Bridge Consequence";
        private const string ScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string PrefabDirectory =
            "Assets/WonderSquad/Prefabs/SleepingForest";
        private const string PrefabPath =
            PrefabDirectory + "/PF_RootBridgeAdvantageSpan.prefab";
        private const string BridgeMaterialPath =
            "Assets/WonderSquad/Art/Materials/Greybox/M_SF_Greybox_Bridge.mat";
        private const string BeaconMaterialPath =
            "Assets/WonderSquad/Art/Materials/Greybox/M_SF_Greybox_Beacon.mat";
        private const string GameplayRootName =
            "ForestSignalRouteConsequence";
        private const string MainBridgeName =
            "RootBridgeTemporaryCrossing";
        private const string MainRouteOuterLegName =
            "RootBridgeMainRouteOuterLeg";
        private const string MainRouteReturnName =
            "RootBridgeMainRouteReturn";
        private const string ObsoleteWestGuardrailName =
            "Boundary_RootBridgeWestGuardrail";
        private const string ObsoleteEastGuardrailName =
            "Boundary_RootBridgeEastGuardrail";
        private const string ObsoleteLeftRootName =
            "RootBridgeLandmark_LeftRoot";
        private const string ObsoleteRightRootName =
            "RootBridgeLandmark_RightRoot";

        private static readonly Vector3 MainBridgePosition =
            new Vector3(8.5f, -0.5f, 23f);
        private static readonly Vector3 MainBridgeScale =
            new Vector3(9f, 1f, 6f);
        private static readonly Vector3 MainRouteOuterLegPosition =
            new Vector3(16f, -0.5f, 34f);
        private static readonly Vector3 MainRouteOuterLegScale =
            new Vector3(6f, 1f, 22f);
        private static readonly Vector3 MainRouteReturnPosition =
            new Vector3(11.5f, -0.5f, 45f);
        private static readonly Vector3 MainRouteReturnScale =
            new Vector3(3f, 1f, 6f);
        private static readonly Vector3 SpanPosition =
            new Vector3(0f, -0.5f, 35f);
        private static readonly Vector3 SpanScale =
            new Vector3(3f, 1f, 16f);
        private static readonly Color ActivatedColor =
            new Color(0.35f, 1f, 0.45f, 1f);
        private static readonly Color DormantColor =
            new Color(0.34f, 0.20f, 0.10f, 1f);

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Sprint003C Root Bridge Consequence",
                    "Exit Play Mode before applying SleepingForest content.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply Sprint003C Root Bridge Consequence?",
                    "This explicit command will create or update only the " +
                    "RootBridgeAdvantageSpan Prefab and its SleepingForest " +
                    "scene composition. The permanent RootBridgeTemporaryCrossing " +
                    "is arranged as the first leg of an always-open outer Main " +
                    "Route. Obsolete guardrails and decorative Root Bridge " +
                    "bars are removed; Player, Camera, FallRecovery and " +
                    "Interaction Foundation are not changed.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            try
            {
                ApplyContent();
                EditorUtility.DisplayDialog(
                    "Sprint003C Root Bridge Consequence",
                    "Root Bridge consequence content was applied and SleepingForest was saved.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Sprint003C Authoring Failed",
                    "No automatic retry was attempted. Review the Console before running the command again.",
                    "OK");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateApplyFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void ApplyForBatchValidation()
        {
            if (!Application.isBatchMode)
            {
                throw new InvalidOperationException(
                    "Batch validation authoring may only run in Unity batch mode.");
            }

            ApplyContent();
        }

        public static void AuditVisibleGeometryForBatchValidation()
        {
            if (!Application.isBatchMode)
            {
                throw new InvalidOperationException(
                    "Batch geometry audit may only run in Unity batch mode.");
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var auditBounds = new Bounds(
                new Vector3(0f, 2f, 38f),
                new Vector3(40f, 20f, 40f));
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var gameObject in GetHierarchyObjects(root))
                {
                    if (!gameObject.TryGetComponent<MeshRenderer>(out var renderer) ||
                        !gameObject.TryGetComponent<MeshFilter>(out _) ||
                        !gameObject.TryGetComponent<BoxCollider>(out var collider) ||
                        !renderer.bounds.Intersects(auditBounds))
                    {
                        continue;
                    }

                    Debug.Log(
                        $"[Sprint003C Geometry Audit] Path={GetHierarchyPath(gameObject.transform)}; " +
                        $"Position={gameObject.transform.position}; " +
                        $"Rotation={gameObject.transform.rotation.eulerAngles}; " +
                        $"Scale={gameObject.transform.lossyScale}; " +
                        $"RendererBounds={renderer.bounds}; " +
                        $"ColliderBounds={GetConfiguredBounds(collider)}");
                }
            }
        }

        private static void ApplyContent()
        {
            EnsureAssetFolder(PrefabDirectory);
            var prefab = CreateOrUpdatePrefab();
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyScene(scene, prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "SleepingForest could not be saved after Sprint003C authoring.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateOrUpdatePrefab()
        {
            var bridgeMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                BridgeMaterialPath);
            var beaconMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                BeaconMaterialPath);
            if (bridgeMaterial == null || beaconMaterial == null)
            {
                throw new InvalidOperationException(
                    "SleepingForest greybox materials are required for Sprint003C authoring.");
            }

            var root = new GameObject("PF_RootBridgeAdvantageSpan");
            root.SetActive(false);
            try
            {
                var ground = CreateCube(
                    "GroundSurface",
                    root.transform,
                    Vector3.zero,
                    SpanScale,
                    bridgeMaterial);
                var dormantMarker = CreateMarker(
                    "DormantRootMarker",
                    root.transform,
                    new Vector3(0f, 1.25f, -6f),
                    new Vector3(1.5f, 1.5f, 1.5f),
                    bridgeMaterial);
                var activatedMarker = CreateMarker(
                    "ActivatedDirectionMarker",
                    root.transform,
                    new Vector3(0f, 1.25f, 6f),
                    new Vector3(1.2f, 2f, 1.2f),
                    beaconMaterial);

                var span = root.AddComponent<RootBridgeAdvantageSpan>();
                var visual = root.AddComponent<RootBridgeAdvantageVisual>();
                if (!span.Configure(
                        ground.GetComponent<Renderer>(),
                        ground.GetComponent<BoxCollider>()) ||
                    !visual.Configure(
                        ground.GetComponent<Renderer>(),
                        dormantMarker,
                        activatedMarker,
                        DormantColor,
                        ActivatedColor))
                {
                    throw new InvalidOperationException(
                        "PF_RootBridgeAdvantageSpan composition is invalid.");
                }

                span.InitializeState();
                visual.InitializeState();
                root.SetActive(true);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "PF_RootBridgeAdvantageSpan could not be saved.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ApplyScene(Scene scene, GameObject prefab)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "SleepingForest must be loaded before Sprint003C authoring.");
            }

            var trial = FindSingleComponent<ForestSignalTrial>(scene);
            var mainBridge = FindSceneObject(scene, MainBridgeName);
            var entrance = FindSceneObject(scene, "RootBridgeEntranceJunction");
            var sliceEnd = FindSceneObject(scene, "SliceEndGround");
            if (trial == null || mainBridge == null || entrance == null ||
                sliceEnd == null)
            {
                throw new InvalidOperationException(
                    "SleepingForest is missing approved Sprint003A/003B scene references.");
            }

            mainBridge.transform.position = MainBridgePosition;
            mainBridge.transform.localScale = MainBridgeScale;
            DestroySceneObject(scene, ObsoleteWestGuardrailName);
            DestroySceneObject(scene, ObsoleteEastGuardrailName);
            DestroySceneObject(scene, ObsoleteLeftRootName);
            DestroySceneObject(scene, ObsoleteRightRootName);

            DestroySceneObject(scene, GameplayRootName);
            var root = new GameObject(GameplayRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.SetActive(false);

            var bridgeMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                BridgeMaterialPath);
            if (bridgeMaterial == null)
            {
                throw new InvalidOperationException(
                    "Root Bridge greybox material is missing.");
            }

            var outerLeg = CreateCube(
                MainRouteOuterLegName,
                root.transform,
                MainRouteOuterLegPosition,
                MainRouteOuterLegScale,
                bridgeMaterial);
            var returnLeg = CreateCube(
                MainRouteReturnName,
                root.transform,
                MainRouteReturnPosition,
                MainRouteReturnScale,
                bridgeMaterial);

            var spanObject = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (spanObject == null)
            {
                throw new InvalidOperationException(
                    "PF_RootBridgeAdvantageSpan could not be instantiated.");
            }

            spanObject.name = "RootBridgeAdvantageSpan";
            spanObject.transform.SetParent(root.transform, false);
            spanObject.transform.position = SpanPosition;
            spanObject.SetActive(false);

            var span = spanObject.GetComponent<RootBridgeAdvantageSpan>();
            var visual = spanObject.GetComponent<RootBridgeAdvantageVisual>();
            if (span == null || visual == null ||
                !span.HasValidConfiguration ||
                !visual.HasValidConfiguration)
            {
                throw new InvalidOperationException(
                    "RootBridgeAdvantageSpan is missing required composition components.");
            }

            var consequence = root.AddComponent<SleepingForestRootBridgeConsequence>();
            if (!consequence.Configure(trial, span, visual))
            {
                throw new InvalidOperationException(
                    "SleepingForest Root Bridge consequence configuration is invalid.");
            }

            spanObject.SetActive(true);
            root.SetActive(true);
            Physics.SyncTransforms();
            ValidateSceneConfiguration(
                scene,
                consequence,
                span,
                mainBridge,
                outerLeg,
                returnLeg,
                entrance,
                sliceEnd);
        }

        private static void ValidateSceneConfiguration(
            Scene scene,
            SleepingForestRootBridgeConsequence consequence,
            RootBridgeAdvantageSpan span,
            GameObject mainBridge,
            GameObject outerLeg,
            GameObject returnLeg,
            GameObject entrance,
            GameObject sliceEnd)
        {
            if (consequence == null || span == null ||
                !consequence.HasValidConfiguration ||
                consequence.Trial == null ||
                mainBridge == null || outerLeg == null || returnLeg == null ||
                entrance == null || sliceEnd == null ||
                !IsInScene(scene, consequence.gameObject) ||
                !IsInScene(scene, span.gameObject) ||
                FindSceneObject(scene, ObsoleteWestGuardrailName) != null ||
                FindSceneObject(scene, ObsoleteEastGuardrailName) != null ||
                FindSceneObject(scene, ObsoleteLeftRootName) != null ||
                FindSceneObject(scene, ObsoleteRightRootName) != null ||
                !HasValidGeometryAudit(
                    span.GroundRenderer.gameObject,
                    entrance,
                    mainBridge,
                    outerLeg,
                    returnLeg,
                    sliceEnd) ||
                !HasWalkableConnection(entrance, mainBridge) ||
                !HasWalkableConnection(mainBridge, outerLeg) ||
                !HasWalkableConnection(outerLeg, returnLeg) ||
                !HasWalkableConnection(returnLeg, sliceEnd) ||
                !HasWalkableConnection(entrance, span.GroundRenderer.gameObject) ||
                !HasWalkableConnection(span.GroundRenderer.gameObject, sliceEnd) ||
                !HasReadableRouteBenefit(
                    entrance,
                    mainBridge,
                    outerLeg,
                    returnLeg,
                    span.GroundRenderer.gameObject,
                    sliceEnd))
            {
                throw new InvalidOperationException(
                    "Sprint003C Root Bridge scene configuration failed validation.");
            }
        }

        private static bool HasWalkableConnection(GameObject from, GameObject to)
        {
            const float minimumConnectionWidth = 1.5f;
            const float edgeTolerance = 0.001f;
            var fromBounds = GetConfiguredBounds(from.GetComponent<BoxCollider>());
            var toBounds = GetConfiguredBounds(to.GetComponent<BoxCollider>());
            var overlapX = Mathf.Min(fromBounds.max.x, toBounds.max.x) -
                           Mathf.Max(fromBounds.min.x, toBounds.min.x);
            var overlapZ = Mathf.Min(fromBounds.max.z, toBounds.max.z) -
                           Mathf.Max(fromBounds.min.z, toBounds.min.z);
            var touchesOnX =
                Mathf.Abs(fromBounds.max.x - toBounds.min.x) <= edgeTolerance ||
                Mathf.Abs(toBounds.max.x - fromBounds.min.x) <= edgeTolerance;
            var touchesOnZ =
                Mathf.Abs(fromBounds.max.z - toBounds.min.z) <= edgeTolerance ||
                Mathf.Abs(toBounds.max.z - fromBounds.min.z) <= edgeTolerance;
            var hasConnection =
                touchesOnX && overlapZ >= minimumConnectionWidth ||
                touchesOnZ && overlapX >= minimumConnectionWidth;
            if (!hasConnection)
            {
                Debug.LogError(
                    $"Invalid route connection: {from.name} {fromBounds} -> " +
                    $"{to.name} {toBounds}; overlap X {overlapX}, " +
                    $"overlap Z {overlapZ}.");
            }

            return hasConnection;
        }

        private static bool HasReadableRouteBenefit(
            GameObject routeStart,
            GameObject mainBridge,
            GameObject outerLeg,
            GameObject returnLeg,
            GameObject advantageSpan,
            GameObject routeEnd)
        {
            const float maximumAdvantageRatio = 0.65f;
            const float minimumParallelSeparation = 10f;
            var start = routeStart.transform.position;
            var end = routeEnd.transform.position;
            var mainLength = CalculatePolylineLength(
                start,
                mainBridge.transform.position,
                outerLeg.transform.position,
                returnLeg.transform.position,
                end);
            var advantageLength = CalculatePolylineLength(
                start,
                advantageSpan.transform.position,
                end);
            var mainBounds = GetConfiguredBounds(
                mainBridge.GetComponent<BoxCollider>());
            var outerBounds = GetConfiguredBounds(
                outerLeg.GetComponent<BoxCollider>());
            var advantageBounds = GetConfiguredBounds(
                advantageSpan.GetComponent<BoxCollider>());
            var parallelSeparation = outerBounds.min.x - advantageBounds.max.x;
            var hasBenefit = advantageLength <= mainLength * maximumAdvantageRatio;
            var hasSpatialSeparation =
                mainBounds.size.x > mainBounds.size.z &&
                advantageBounds.size.z > advantageBounds.size.x &&
                parallelSeparation >= minimumParallelSeparation;
            if (!hasBenefit || !hasSpatialSeparation)
            {
                Debug.LogError(
                    $"Advantage route length {advantageLength} must be no more " +
                    $"than {maximumAdvantageRatio:P0} of Main Route length " +
                    $"{mainLength}; parallel separation is " +
                    $"{parallelSeparation}.");
            }

            return hasBenefit && hasSpatialSeparation;
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

        private static bool HasValidGeometryAudit(
            params GameObject[] geometryObjects)
        {
            for (var firstIndex = 0; firstIndex < geometryObjects.Length; firstIndex++)
            {
                var firstObject = geometryObjects[firstIndex];
                var firstRenderer = firstObject.GetComponent<Renderer>();
                var firstCollider = firstObject.GetComponent<BoxCollider>();
                if (firstRenderer == null || firstCollider == null)
                {
                    Debug.LogError(
                        $"Geometry audit requires Renderer and BoxCollider: " +
                        firstObject.name);
                    return false;
                }

                for (var secondIndex = firstIndex + 1;
                     secondIndex < geometryObjects.Length;
                     secondIndex++)
                {
                    var secondObject = geometryObjects[secondIndex];
                    var secondRenderer = secondObject.GetComponent<Renderer>();
                    var secondCollider = secondObject.GetComponent<BoxCollider>();
                    if (secondRenderer == null || secondCollider == null ||
                        !IsNoPositiveVolumeOverlap(
                            firstRenderer.bounds,
                            secondRenderer.bounds) ||
                        !IsNoPositiveVolumeOverlap(
                            GetConfiguredBounds(firstCollider),
                            GetConfiguredBounds(secondCollider)))
                    {
                        Debug.LogError(
                            $"Geometry overlap or missing component: " +
                            $"{firstObject.name} {firstRenderer.bounds} / " +
                            $"{secondObject.name} " +
                            $"{(secondRenderer == null ? default : secondRenderer.bounds)}.");
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsNoPositiveVolumeOverlap(Bounds first, Bounds second)
        {
            return !(first.min.x < second.max.x && first.max.x > second.min.x &&
                     first.min.y < second.max.y && first.max.y > second.min.y &&
                     first.min.z < second.max.z && first.max.z > second.min.z);
        }

        private static Bounds GetConfiguredBounds(BoxCollider collider)
        {
            var scale = collider.transform.lossyScale;
            var absoluteScale = new Vector3(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y),
                Mathf.Abs(scale.z));
            return new Bounds(
                collider.transform.TransformPoint(collider.center),
                Vector3.Scale(collider.size, absoluteScale));
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            return cube;
        }

        private static GameObject CreateMarker(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var marker = CreateCube(name, parent, localPosition, localScale, material);
            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            return marker;
        }

        private static void DestroySceneObject(Scene scene, string name)
        {
            var existing = FindSceneObject(scene, name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static T FindSingleComponent<T>(Scene scene)
            where T : Component
        {
            var components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            return components.Length == 1 ? components[0] : null;
        }

        private static GameObject FindSceneObject(Scene scene, string name)
        {
            return scene.GetRootGameObjects()
                .SelectMany(GetHierarchyObjects)
                .FirstOrDefault(gameObject => gameObject.name == name);
        }

        private static System.Collections.Generic.IEnumerable<GameObject>
            GetHierarchyObjects(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                yield return transform.gameObject;
            }
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

        private static bool IsInScene(Scene scene, GameObject gameObject)
        {
            return gameObject != null && gameObject.scene == scene;
        }

        private static void EnsureAssetFolder(string path)
        {
            var parts = path.Split('/');
            var currentPath = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var nextPath = currentPath + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
