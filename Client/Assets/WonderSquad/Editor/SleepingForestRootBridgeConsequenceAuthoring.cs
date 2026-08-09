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
        private const string StartJunctionName =
            "RootBridgeAdvantageStartJunction";
        private const string ExitJunctionName =
            "RootBridgeAdvantageExitJunction";

        private static readonly Vector3 StartJunctionPosition =
            new Vector3(-5f, -0.5f, 23f);
        private static readonly Vector3 StartJunctionScale =
            new Vector3(2f, 1f, 8f);
        private static readonly Vector3 SpanPosition =
            new Vector3(-5f, -0.5f, 34.5f);
        private static readonly Vector3 SpanScale =
            new Vector3(2f, 1f, 15f);
        private static readonly Vector3 ExitJunctionPosition =
            new Vector3(-7f, -0.5f, 42.5f);
        private static readonly Vector3 ExitJunctionScale =
            new Vector3(6f, 1f, 1f);
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
                    "scene composition. RootBridgeTemporaryCrossing, Player, " +
                    "Camera, FallRecovery and Interaction Foundation are not changed.",
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
            var mainBridge = FindSceneObject(scene, "RootBridgeTemporaryCrossing");
            var entrance = FindSceneObject(scene, "RootBridgeEntranceJunction");
            var sliceEnd = FindSceneObject(scene, "SliceEndGround");
            if (trial == null || mainBridge == null || entrance == null || sliceEnd == null)
            {
                throw new InvalidOperationException(
                    "SleepingForest is missing approved Sprint003A/003B scene references.");
            }

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

            CreateCube(
                StartJunctionName,
                root.transform,
                StartJunctionPosition,
                StartJunctionScale,
                bridgeMaterial);
            CreateCube(
                ExitJunctionName,
                root.transform,
                ExitJunctionPosition,
                ExitJunctionScale,
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
            ValidateSceneConfiguration(
                scene,
                consequence,
                span,
                mainBridge,
                entrance,
                sliceEnd,
                FindSceneObject(scene, StartJunctionName),
                FindSceneObject(scene, ExitJunctionName));
        }

        private static void ValidateSceneConfiguration(
            Scene scene,
            SleepingForestRootBridgeConsequence consequence,
            RootBridgeAdvantageSpan span,
            GameObject mainBridge,
            GameObject entrance,
            GameObject sliceEnd,
            GameObject startJunction,
            GameObject exitJunction)
        {
            if (consequence == null || span == null ||
                !consequence.HasValidConfiguration ||
                consequence.Trial == null ||
                mainBridge == null || entrance == null || sliceEnd == null ||
                startJunction == null || exitJunction == null ||
                !IsInScene(scene, consequence.gameObject) ||
                !IsInScene(scene, span.gameObject) ||
                !HasValidGeometryAudit(
                    span.GroundRenderer.gameObject,
                    startJunction,
                    exitJunction,
                    entrance,
                    mainBridge,
                    sliceEnd))
            {
                throw new InvalidOperationException(
                    "Sprint003C Root Bridge scene configuration failed validation.");
            }
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
                            firstCollider.bounds,
                            secondCollider.bounds))
                    {
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
