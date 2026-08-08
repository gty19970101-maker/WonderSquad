using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Prompt;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.Editor
{
    /// <summary>
    /// Applies the explicitly approved Sprint003B content composition to the
    /// formal SleepingForest scene. It never runs automatically.
    /// </summary>
    public static class SleepingForestSignalTrialAuthoring
    {
        private const string MenuPath =
            "Wonder Squad/Sprint003B/Apply Forest Signal Trial";
        private const string ScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string DefinitionDirectory =
            "Assets/WonderSquad/ScriptableObjects/Content/SleepingForest";
        private const string DefinitionPath =
            DefinitionDirectory + "/ForestSignalTrialDefinition.asset";
        private const string PromptPath =
            "Assets/WonderSquad/ScriptableObjects/Content/Interaction/InteractionPrompt_ForestBeacon.asset";
        private const string PrefabDirectory =
            "Assets/WonderSquad/Prefabs/SleepingForest";
        private const string PrefabPath =
            PrefabDirectory + "/PF_ForestBeacon.prefab";
        private const string BeaconMaterialPath =
            "Assets/WonderSquad/Art/Materials/Greybox/M_SF_Greybox_Beacon.mat";
        private const string TrialStableId =
            "sleeping_forest.forest_signal_trial";
        private const string BeaconATargetId =
            "sleeping_forest.beacon.a";
        private const string BeaconBTargetId =
            "sleeping_forest.beacon.b";
        private const string PromptStableId =
            "sleeping_forest.beacon.activate";
        private const string PromptLocalizationKey =
            "interaction.sleeping_forest.beacon.activate";
        private const string PromptFallbackText =
            "Activate Forest Beacon";
        private const int DefinitionSchemaVersion = 1;
        private const int InteractableLayer = 8;
        private const int BeaconDetectionPriority = 10;
        private const float BeaconBodyCenterY = 3.5f;

        private static readonly Vector3 DetectionAnchorOffset =
            new Vector3(0f, 1.5f, -2.5f);
        private static readonly Color DormantColor =
            new Color(0.25f, 0.55f, 0.65f, 1f);
        private static readonly Color ActivatedColor =
            new Color(0.35f, 1f, 0.45f, 1f);
        private static readonly Color IncorrectColor =
            new Color(1f, 0.3f, 0.2f, 1f);

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Sprint003B Forest Signal Trial",
                    "Exit Play Mode before applying formal scene content.",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Apply Sprint003B Forest Signal Trial?",
                    "This explicit command will create or update the approved " +
                    "Beacon Definition, Prompt, Prefab, and the two formal " +
                    "SleepingForest Beacon instances. It will not change routes, " +
                    "Root Bridge, Slice End, Character, or Interaction Foundation.",
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
                    "Sprint003B Forest Signal Trial",
                    "Forest Signal Trial content was applied and the formal scene was saved.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Sprint003B Authoring Failed",
                    "No automatic retry was attempted. Review the Console before running the command again.",
                    "OK");
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateApplyFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        /// <summary>
        /// Applies the same explicit authoring operation in an isolated
        /// batch-mode project. The caller is responsible for using a copy,
        /// never a project that is already open in Unity.
        /// </summary>
        public static void ApplyInIsolatedBatchMode()
        {
            if (!Application.isBatchMode)
            {
                throw new InvalidOperationException(
                    "Isolated authoring entry requires Unity batch mode.");
            }

            ApplyContent();
        }

        private static void ApplyContent()
        {
            EnsureAssetFolder(DefinitionDirectory);
            EnsureAssetFolder(PrefabDirectory);

            var definition = GetOrCreateDefinition();
            var prompt = GetOrCreatePrompt();
            AssetDatabase.SaveAssets();
            var prefab = CreateOrUpdatePrefab(prompt);

            var scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            ApplyScene(scene, definition, prompt, prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    "SleepingForest scene could not be saved after Sprint003B authoring.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static ForestSignalTrialDefinition GetOrCreateDefinition()
        {
            var definition =
                AssetDatabase.LoadAssetAtPath<ForestSignalTrialDefinition>(
                    DefinitionPath);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<ForestSignalTrialDefinition>();
                definition.name = "ForestSignalTrialDefinition";
                AssetDatabase.CreateAsset(definition, DefinitionPath);
            }

            if (!definition.Configure(
                    TrialStableId,
                    DefinitionSchemaVersion,
                    BeaconATargetId,
                    BeaconBTargetId))
            {
                throw new InvalidOperationException(
                    "Forest Signal Trial definition values are invalid.");
            }

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static InteractionPromptDefinition GetOrCreatePrompt()
        {
            var prompt =
                AssetDatabase.LoadAssetAtPath<InteractionPromptDefinition>(
                    PromptPath);
            if (prompt == null)
            {
                prompt =
                    ScriptableObject.CreateInstance<InteractionPromptDefinition>();
                prompt.name = "InteractionPrompt_ForestBeacon";
                AssetDatabase.CreateAsset(prompt, PromptPath);
            }

            if (!prompt.Configure(
                    PromptStableId,
                    PromptLocalizationKey,
                    PromptFallbackText))
            {
                throw new InvalidOperationException(
                    "Forest Beacon prompt definition values are invalid.");
            }

            EditorUtility.SetDirty(prompt);
            return prompt;
        }

        private static GameObject CreateOrUpdatePrefab(
            InteractionPromptDefinition prompt)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                BeaconMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException(
                    "SleepingForest beacon greybox material is missing.");
            }

            var root = new GameObject("PF_ForestBeacon");
            root.SetActive(false);
            SetLayerRecursively(root, InteractableLayer);
            try
            {
                var detectionAnchor = CreateEmpty(
                    "DetectionAnchor",
                    root.transform,
                    DetectionAnchorOffset);
                var detectionVolume = CreateEmpty(
                    "DetectionVolume",
                    root.transform,
                    DetectionAnchorOffset);
                var trigger = detectionVolume.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.9f;

                var body = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "BeaconBody",
                    root.transform,
                    new Vector3(0f, BeaconBodyCenterY, 0f),
                    new Vector3(2.2f, 3.5f, 2.2f),
                    material);
                RemoveCollider(body);

                var dormantMarker = CreatePrimitive(
                    PrimitiveType.Sphere,
                    "DormantMarker",
                    root.transform,
                    new Vector3(0f, 7.3f, 0f),
                    new Vector3(0.75f, 0.75f, 0.75f),
                    material);
                RemoveCollider(dormantMarker);

                var activatedMarker = CreatePrimitive(
                    PrimitiveType.Cylinder,
                    "ActivatedRing",
                    root.transform,
                    new Vector3(0f, 7.3f, 0f),
                    new Vector3(1.5f, 0.15f, 1.5f),
                    material);
                RemoveCollider(activatedMarker);

                var incorrectMarker = CreateIncorrectMarker(
                    root.transform,
                    material);

                var target = root.AddComponent<InteractionTarget>();
                target.Configure(
                    "sleeping_forest.beacon.template",
                    detectionAnchor.transform,
                    true,
                    BeaconDetectionPriority);

                var promptSource =
                    root.AddComponent<InteractionPromptSource>();
                promptSource.Configure(prompt);

                var interaction =
                    root.AddComponent<ForestBeaconInteraction>();
                interaction.Configure(
                    target,
                    null,
                    ForestBeaconSlot.Unknown);

                var visual = root.AddComponent<ForestBeaconVisual>();
                visual.Configure(
                    null,
                    ForestBeaconSlot.Unknown,
                    body.GetComponent<Renderer>(),
                    dormantMarker,
                    activatedMarker,
                    incorrectMarker,
                    DormantColor,
                    ActivatedColor,
                    IncorrectColor);

                root.SetActive(true);
                target.enabled = true;
                SetLayerRecursively(root, InteractableLayer);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "PF_ForestBeacon could not be saved.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ApplyScene(
            Scene scene,
            ForestSignalTrialDefinition definition,
            InteractionPromptDefinition prompt,
            GameObject prefab)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "SleepingForest scene is not loaded for authoring.");
            }

            var beaconAWorldPosition = ResolveBeaconRootPosition(
                scene,
                "ForestBeacon_A",
                "BeaconA_Reserved",
                "BeaconA_Reserved_InteractionAnchor_Reserved");
            var beaconBWorldPosition = ResolveBeaconRootPosition(
                scene,
                "ForestBeacon_B",
                "BeaconB_Reserved",
                "BeaconB_Reserved_InteractionAnchor_Reserved");

            DestroySceneObject(scene, "ForestBeacon_A");
            DestroySceneObject(scene, "ForestBeacon_B");
            DestroySceneObject(scene, "ForestSignalTrialGameplay");
            DestroySceneObject(scene, "BeaconA_Reserved");
            DestroySceneObject(scene, "BeaconB_Reserved");
            DestroySceneObject(
                scene,
                "BeaconA_Reserved_InteractionAnchor_Reserved");
            DestroySceneObject(
                scene,
                "BeaconB_Reserved_InteractionAnchor_Reserved");

            var gameplayRoot = new GameObject("ForestSignalTrialGameplay");
            SceneManager.MoveGameObjectToScene(gameplayRoot, scene);
            gameplayRoot.SetActive(false);

            var trialObject = new GameObject("ForestSignalTrial");
            SceneManager.MoveGameObjectToScene(trialObject, scene);
            trialObject.transform.SetParent(gameplayRoot.transform, false);
            var trial = trialObject.AddComponent<ForestSignalTrial>();

            var beaconA = CreateSceneBeacon(
                scene,
                prefab,
                gameplayRoot.transform,
                "ForestBeacon_A",
                beaconAWorldPosition,
                BeaconATargetId,
                ForestBeaconSlot.BeaconA,
                prompt,
                trial);
            var beaconB = CreateSceneBeacon(
                scene,
                prefab,
                gameplayRoot.transform,
                "ForestBeacon_B",
                beaconBWorldPosition,
                BeaconBTargetId,
                ForestBeaconSlot.BeaconB,
                prompt,
                trial);

            if (!trial.Configure(definition, beaconA, beaconB))
            {
                throw new InvalidOperationException(
                    "ForestSignalTrial scene configuration is invalid.");
            }

            PersistObjectReference(trial, "trialDefinition", definition);
            PersistObjectReference(trial, "beaconA", beaconA);
            PersistObjectReference(trial, "beaconB", beaconB);
            ValidateSceneTrialConfiguration(
                scene,
                definition,
                trial,
                beaconA,
                beaconB);
            beaconA.gameObject.SetActive(true);
            beaconB.gameObject.SetActive(true);
            gameplayRoot.SetActive(true);
        }

        private static ForestBeaconInteraction CreateSceneBeacon(
            Scene scene,
            GameObject prefab,
            Transform parent,
            string instanceName,
            Vector3 position,
            string targetId,
            ForestBeaconSlot slot,
            InteractionPromptDefinition prompt,
            ForestSignalTrial trial)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene)
                as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "PF_ForestBeacon could not be instantiated in SleepingForest.");
            }

            instance.name = instanceName;
            instance.transform.SetParent(parent, true);
            instance.transform.position = position;
            instance.SetActive(false);

            var target = instance.GetComponent<InteractionTarget>();
            var promptSource = instance.GetComponent<InteractionPromptSource>();
            var interaction =
                instance.GetComponent<ForestBeaconInteraction>();
            var visual = instance.GetComponent<ForestBeaconVisual>();
            var detectionAnchor = FindChild(instance, "DetectionAnchor");
            var body = FindChild(instance, "BeaconBody");
            var dormantMarker = FindChild(instance, "DormantMarker");
            var activatedMarker = FindChild(instance, "ActivatedRing");
            var incorrectMarker = FindChild(instance, "IncorrectMarker");
            if (target == null ||
                promptSource == null ||
                interaction == null ||
                visual == null ||
                detectionAnchor == null ||
                body == null ||
                dormantMarker == null ||
                activatedMarker == null ||
                incorrectMarker == null)
            {
                throw new InvalidOperationException(
                    "PF_ForestBeacon is missing required Sprint003B composition.");
            }

            target.enabled = true;
            target.Configure(
                targetId,
                detectionAnchor.transform,
                true,
                BeaconDetectionPriority);
            promptSource.Configure(prompt);
            interaction.Configure(target, trial, slot);
            visual.Configure(
                trial,
                slot,
                body.GetComponent<Renderer>(),
                dormantMarker,
                activatedMarker,
                incorrectMarker,
                DormantColor,
                ActivatedColor,
                IncorrectColor);
            SetLayerRecursively(instance, InteractableLayer);

            PersistObjectReference(
                interaction,
                "interactionTargetComponent",
                target);
            PersistObjectReference(interaction, "trial", trial);
            PersistEnum(interaction, "slot", (int)slot);
            PersistObjectReference(visual, "trial", trial);
            PersistEnum(visual, "slot", (int)slot);
            PersistComponent(target);
            PersistComponent(promptSource);
            PersistComponent(interaction);
            PersistComponent(visual);
            return interaction;
        }

        private static void ValidateSceneTrialConfiguration(
            Scene scene,
            ForestSignalTrialDefinition definition,
            ForestSignalTrial trial,
            ForestBeaconInteraction beaconA,
            ForestBeaconInteraction beaconB)
        {
            if (definition == null ||
                trial == null ||
                beaconA == null ||
                beaconB == null ||
                beaconA == beaconB ||
                trial.gameObject.scene != scene ||
                beaconA.gameObject.scene != scene ||
                beaconB.gameObject.scene != scene ||
                trial.TrialDefinition != definition ||
                trial.BeaconA != beaconA ||
                trial.BeaconB != beaconB ||
                !trial.HasValidConfiguration)
            {
                throw new InvalidOperationException(
                    "ForestSignalTrial scene references were not fully persisted.");
            }
        }

        private static void PersistObjectReference(
            UnityEngine.Object target,
            string propertyPath,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyPath + "' on " +
                    target.GetType().Name + ".");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PersistObject(target);
        }

        private static void PersistEnum(
            UnityEngine.Object target,
            string propertyPath,
            int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyPath + "' on " +
                    target.GetType().Name + ".");
            }

            property.enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PersistObject(target);
        }

        private static void PersistComponent(Component component)
        {
            PersistObject(component);
        }

        private static void PersistObject(UnityEngine.Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }

        private static Vector3 ResolveBeaconRootPosition(
            Scene scene,
            string currentName,
            string reservedVisualName,
            string reservedAnchorName)
        {
            var current = FindSceneObject(scene, currentName);
            if (current != null)
            {
                return current.transform.position;
            }

            var reservedVisual =
                FindSceneObject(scene, reservedVisualName);
            if (reservedVisual != null)
            {
                return reservedVisual.transform.position -
                       Vector3.up * BeaconBodyCenterY;
            }

            var reservedAnchor =
                FindSceneObject(scene, reservedAnchorName);
            if (reservedAnchor != null)
            {
                return reservedAnchor.transform.position -
                       DetectionAnchorOffset;
            }

            throw new InvalidOperationException(
                "SleepingForest is missing the reserved placement for " +
                currentName + ".");
        }

        private static GameObject CreateIncorrectMarker(
            Transform parent,
            Material material)
        {
            var marker = CreateEmpty(
                "IncorrectMarker",
                parent,
                new Vector3(0f, 7.3f, 0f));
            var firstBar = CreatePrimitive(
                PrimitiveType.Cube,
                "IncorrectBar_A",
                marker.transform,
                Vector3.zero,
                new Vector3(1.8f, 0.25f, 0.25f),
                material);
            firstBar.transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            RemoveCollider(firstBar);
            var secondBar = CreatePrimitive(
                PrimitiveType.Cube,
                "IncorrectBar_B",
                marker.transform,
                Vector3.zero,
                new Vector3(1.8f, 0.25f, 0.25f),
                material);
            secondBar.transform.localRotation =
                Quaternion.Euler(0f, 0f, -45f);
            RemoveCollider(secondBar);
            return marker;
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            SetLayerRecursively(primitive, InteractableLayer);
            return primitive;
        }

        private static GameObject CreateEmpty(
            string name,
            Transform parent,
            Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.layer = InteractableLayer;
            return child;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static GameObject FindChild(
            GameObject root,
            string name)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == name)
                ?.gameObject;
        }

        private static GameObject FindSceneObject(
            Scene scene,
            string name)
        {
            return scene
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(child => child.name == name)
                ?.gameObject;
        }

        private static void DestroySceneObject(
            Scene scene,
            string name)
        {
            var gameObject = FindSceneObject(scene, name);
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetLayerRecursively(
            GameObject root,
            int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var currentPath = "Assets";
            var pathParts = assetPath.Split('/');
            for (var index = 1; index < pathParts.Length; index++)
            {
                var nextPath = currentPath + "/" + pathParts[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(
                        currentPath,
                        pathParts[index]);
                }

                currentPath = nextPath;
            }
        }
    }
}
