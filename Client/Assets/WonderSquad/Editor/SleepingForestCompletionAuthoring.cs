using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WonderSquad.Player.Spawning;
using WonderSquad.Puzzle.SleepingForest;
using WonderSquad.SleepingForest.Completion;
using WonderSquad.UI.SleepingForest;

namespace WonderSquad.Editor
{
    /// <summary>
    /// Applies the explicitly approved Sprint003D completion composition.
    /// It is menu-only and never runs during project initialization.
    /// </summary>
    public static class SleepingForestCompletionAuthoring
    {
        private const string MenuPath =
            "Wonder Squad/Sprint003D/Apply Sleeping Forest Completion";
        private const string ScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string PrefabDirectory =
            "Assets/WonderSquad/Prefabs/UI";
        private const string PrefabPath =
            PrefabDirectory + "/PF_SleepingForestCompletionFeedback.prefab";
        private const string MarkerMaterialPath =
            "Assets/WonderSquad/Art/Materials/Greybox/M_SF_Greybox_Beacon.mat";
        private const string CompletionRootName =
            "SleepingForestCompletion";
        private const string TriggerName =
            "SleepingForestSliceEndTrigger";
        private const string MarkerViewName =
            "SleepingForestCompletionMarkerView";
        private const string CompletedMarkerName =
            "SleepingForestCompletedMarker";
        private const string CanvasName =
            "SleepingForestCompletionCanvas";
        private const string ContentRootName =
            "CompletionContent";
        private const string MessageTextName =
            "MessageText";

        private static readonly Vector3 TriggerPosition =
            new Vector3(0f, 1.5f, 54.75f);
        private static readonly Vector3 TriggerSize =
            new Vector3(16f, 3f, 2.5f);
        private static readonly Vector3 MarkerPosition =
            new Vector3(0f, 3.25f, 55.5f);
        private static readonly Vector3 MarkerScale =
            new Vector3(1.25f, 1.25f, 1.25f);

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Sprint003D Sleeping Forest Completion",
                    "Exit Play Mode before applying formal scene content.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply Sprint003D Sleeping Forest Completion?",
                    "This explicit command creates or updates only the " +
                    "approved Slice End Trigger, scene-local completion " +
                    "controller and minimal feedback. Sprint003A-C and " +
                    "Foundation objects are preserved.",
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
                    "Sprint003D Sleeping Forest Completion",
                    "Completion content was applied and SleepingForest was saved.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Sprint003D Authoring Failed",
                    "No automatic retry was attempted. Review the Console " +
                    "before running the command again.",
                    "OK");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateApplyFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>
        /// Explicit CI entry used only in an isolated batch-mode project copy.
        /// It never runs during normal Editor initialization.
        /// </summary>
        public static void ApplyForBatchValidation()
        {
            if (!Application.isBatchMode)
            {
                throw new InvalidOperationException(
                    "Batch validation authoring is unavailable in an " +
                    "interactive Unity Editor. Use the confirmed menu command.");
            }

            ApplyContent();
        }

        private static void ApplyContent()
        {
            EnsureAssetFolder(PrefabDirectory);
            var feedbackPrefab = CreateOrUpdateFeedbackPrefab();
            var scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            ApplyScene(scene, feedbackPrefab);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "SleepingForest could not be saved after Sprint003D authoring.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateOrUpdateFeedbackPrefab()
        {
            var root = new GameObject(
                "PF_SleepingForestCompletionFeedback",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(SleepingForestCompletionView),
                typeof(SleepingForestCompletionPresenter));
            root.layer = 5;
            root.SetActive(false);
            try
            {
                var rectTransform = root.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 60;

                var canvasScaler = root.GetComponent<CanvasScaler>();
                canvasScaler.uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution =
                    new Vector2(1920f, 1080f);
                canvasScaler.matchWidthOrHeight = 0.5f;

                var contentRoot = CreateContentRoot(root.transform);
                var messageText = CreateMessageText(contentRoot.transform);
                SetObjectReference(
                    root.GetComponent<SleepingForestCompletionView>(),
                    "contentRoot",
                    contentRoot);
                SetObjectReference(
                    root.GetComponent<SleepingForestCompletionView>(),
                    "messageText",
                    messageText);
                contentRoot.SetActive(false);

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "PF_SleepingForestCompletionFeedback could not be saved.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ApplyScene(Scene scene, GameObject feedbackPrefab)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "SleepingForest must be loaded before Sprint003D authoring.");
            }

            var playerSpawner = FindSingleComponent<PlayerSpawner>(scene);
            var trial = FindSingleComponent<ForestSignalTrial>(scene);
            var sliceEndGround = FindSceneObject(scene, "SliceEndGround");
            var sliceEndTower = FindSceneObject(
                scene,
                "SliceEndLandmark_Tower");
            var sliceEndArch = FindSceneObject(
                scene,
                "SliceEndLandmark_Arch");
            if (playerSpawner == null || trial == null ||
                sliceEndGround == null || sliceEndTower == null ||
                sliceEndArch == null)
            {
                throw new InvalidOperationException(
                    "SleepingForest is missing an approved Sprint003A/003B " +
                    "scene reference.");
            }

            var completionRoot = GetOrCreateUniqueSceneObject(
                scene,
                CompletionRootName);
            completionRoot.SetActive(false);
            completionRoot.transform.SetPositionAndRotation(
                Vector3.zero,
                Quaternion.identity);
            completionRoot.transform.localScale = Vector3.one;

            var triggerObject = GetOrCreateUniqueChild(
                completionRoot.transform,
                TriggerName);
            triggerObject.transform.position = TriggerPosition;
            triggerObject.transform.rotation = Quaternion.identity;
            triggerObject.transform.localScale = Vector3.one;
            var triggerCollider = GetOrAddSingleComponent<BoxCollider>(
                triggerObject);
            triggerCollider.center = Vector3.zero;
            triggerCollider.size = TriggerSize;
            triggerCollider.isTrigger = true;
            var sliceEndTrigger =
                GetOrAddSingleComponent<SleepingForestSliceEndTrigger>(
                    triggerObject);
            SetObjectReference(
                sliceEndTrigger,
                "playerSpawner",
                playerSpawner);
            SetObjectReference(
                sliceEndTrigger,
                "triggerCollider",
                triggerCollider);

            var markerViewObject = GetOrCreateUniqueChild(
                completionRoot.transform,
                MarkerViewName);
            var completedMarker = GetOrCreateMarker(
                markerViewObject.transform);
            var markerView =
                GetOrAddSingleComponent<SleepingForestCompletionMarkerView>(
                    markerViewObject);
            SetObjectReference(
                markerView,
                "completedMarker",
                completedMarker);

            var canvasObject = GetOrCreateFeedbackCanvas(
                scene,
                completionRoot.transform,
                feedbackPrefab);
            canvasObject.name = CanvasName;
            var completionView =
                GetSingleComponentInChildren<SleepingForestCompletionView>(
                    canvasObject);
            var presenter =
                GetSingleComponentInChildren<SleepingForestCompletionPresenter>(
                    canvasObject);
            var controller =
                GetOrAddSingleComponent<SleepingForestCompletionController>(
                    completionRoot);

            SetObjectReference(
                controller,
                "forestSignalTrial",
                trial);
            SetObjectReference(
                controller,
                "sliceEndTrigger",
                sliceEndTrigger);
            SetObjectReference(
                presenter,
                "completionController",
                controller);
            SetObjectReference(
                presenter,
                "completionView",
                completionView);
            SetObjectReference(
                presenter,
                "markerView",
                markerView);
            PrefabUtility.RecordPrefabInstancePropertyModifications(
                presenter);

            triggerObject.SetActive(true);
            markerViewObject.SetActive(true);
            canvasObject.SetActive(true);
            completionRoot.SetActive(true);

            ValidateSceneConfiguration(
                scene,
                completionRoot,
                playerSpawner,
                trial,
                controller,
                sliceEndTrigger,
                triggerCollider,
                presenter,
                completionView,
                markerView,
                completedMarker,
                sliceEndGround,
                sliceEndTower,
                sliceEndArch);
        }

        private static void ValidateSceneConfiguration(
            Scene scene,
            GameObject completionRoot,
            PlayerSpawner playerSpawner,
            ForestSignalTrial trial,
            SleepingForestCompletionController controller,
            SleepingForestSliceEndTrigger sliceEndTrigger,
            BoxCollider triggerCollider,
            SleepingForestCompletionPresenter presenter,
            SleepingForestCompletionView completionView,
            SleepingForestCompletionMarkerView markerView,
            GameObject completedMarker,
            GameObject sliceEndGround,
            GameObject sliceEndTower,
            GameObject sliceEndArch)
        {
            if (CountComponents<SleepingForestCompletionController>(scene) != 1 ||
                CountComponents<SleepingForestSliceEndTrigger>(scene) != 1 ||
                CountComponents<SleepingForestCompletionPresenter>(scene) != 1 ||
                completionRoot == null ||
                playerSpawner == null ||
                trial == null ||
                controller == null || !controller.HasValidConfiguration ||
                sliceEndTrigger == null ||
                !sliceEndTrigger.HasValidConfiguration ||
                triggerCollider == null || !triggerCollider.isTrigger ||
                presenter == null || !presenter.HasValidConfiguration ||
                completionView == null ||
                !completionView.HasValidConfiguration ||
                markerView == null || !markerView.HasValidConfiguration ||
                completedMarker == null ||
                completedMarker.GetComponent<Collider>() != null ||
                sliceEndGround == null ||
                sliceEndTower == null ||
                sliceEndArch == null ||
                !IsInScene(scene, completionRoot) ||
                !triggerCollider.bounds.Intersects(
                    sliceEndGround.GetComponent<Renderer>().bounds))
            {
                throw new InvalidOperationException(
                    "Sprint003D SleepingForest completion configuration " +
                    "failed validation.");
            }
        }

        private static GameObject CreateContentRoot(Transform parent)
        {
            var content = new GameObject(
                ContentRootName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            content.layer = 5;
            content.transform.SetParent(parent, false);
            var rectTransform = content.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -110f);
            rectTransform.sizeDelta = new Vector2(620f, 96f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var background = content.GetComponent<Image>();
            background.color = new Color(0.035f, 0.055f, 0.075f, 0.9f);
            background.raycastTarget = false;
            return content;
        }

        private static Text CreateMessageText(Transform parent)
        {
            var textObject = new GameObject(
                MessageTextName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.layer = 5;
            textObject.transform.SetParent(parent, false);
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(24f, 12f);
            rectTransform.offsetMax = new Vector2(-24f, -12f);

            var messageText = textObject.GetComponent<Text>();
            messageText.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            messageText.fontSize = 30;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = Color.white;
            messageText.raycastTarget = false;
            messageText.text = string.Empty;
            return messageText;
        }

        private static GameObject GetOrCreateMarker(Transform parent)
        {
            var marker = GetOrCreateUniqueChild(parent, CompletedMarkerName);
            marker.transform.position = MarkerPosition;
            marker.transform.rotation = Quaternion.identity;
            marker.transform.localScale = MarkerScale;

            var meshFilter = marker.GetComponent<MeshFilter>();
            var meshRenderer = marker.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null)
            {
                var temporaryCube = GameObject.CreatePrimitive(
                    PrimitiveType.Cube);
                if (meshFilter == null)
                {
                    meshFilter = marker.AddComponent<MeshFilter>();
                }

                if (meshRenderer == null)
                {
                    meshRenderer = marker.AddComponent<MeshRenderer>();
                }

                meshFilter.sharedMesh =
                    temporaryCube.GetComponent<MeshFilter>().sharedMesh;
                UnityEngine.Object.DestroyImmediate(temporaryCube);
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(
                MarkerMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException(
                    "SleepingForest completion marker material is missing.");
            }

            meshRenderer.sharedMaterial = material;
            foreach (var collider in marker.GetComponents<Collider>())
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            marker.SetActive(false);
            return marker;
        }

        private static GameObject GetOrCreateFeedbackCanvas(
            Scene scene,
            Transform parent,
            GameObject feedbackPrefab)
        {
            var matches = parent
                .GetComponentsInChildren<SleepingForestCompletionView>(true)
                .Select(component => component.gameObject)
                .Distinct()
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    "SleepingForest contains duplicate Sprint003D completion views.");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            var canvasObject =
                PrefabUtility.InstantiatePrefab(feedbackPrefab, scene) as GameObject;
            if (canvasObject == null)
            {
                throw new InvalidOperationException(
                    "Completion feedback Prefab could not be instantiated.");
            }

            canvasObject.SetActive(false);
            canvasObject.transform.SetParent(parent, false);
            return canvasObject;
        }

        private static GameObject GetOrCreateUniqueSceneObject(
            Scene scene,
            string name)
        {
            var matches = scene.GetRootGameObjects()
                .SelectMany(GetHierarchyObjects)
                .Where(gameObject => gameObject.name == name)
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    "SleepingForest contains duplicate object: " + name);
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root;
        }

        private static GameObject GetOrCreateUniqueChild(
            Transform parent,
            string name)
        {
            var matches = parent
                .GetComponentsInChildren<Transform>(true)
                .Where(transform => transform != parent && transform.name == name)
                .Select(transform => transform.gameObject)
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException(
                    "SleepingForest completion root contains duplicate object: " +
                    name);
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T GetOrAddSingleComponent<T>(GameObject gameObject)
            where T : Component
        {
            var components = gameObject.GetComponents<T>();
            if (components.Length > 1)
            {
                throw new InvalidOperationException(
                    gameObject.name + " contains duplicate " + typeof(T).Name);
            }

            return components.Length == 1
                ? components[0]
                : gameObject.AddComponent<T>();
        }

        private static T GetSingleComponentInChildren<T>(GameObject root)
            where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            if (components.Length != 1)
            {
                throw new InvalidOperationException(
                    root.name + " must contain exactly one " + typeof(T).Name);
            }

            return components[0];
        }

        private static T FindSingleComponent<T>(Scene scene)
            where T : Component
        {
            var components = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
            return components.Length == 1 ? components[0] : null;
        }

        private static int CountComponents<T>(Scene scene)
            where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .Count();
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

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    target.GetType().Name + " is missing serialized property " +
                    propertyName + ".");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
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
