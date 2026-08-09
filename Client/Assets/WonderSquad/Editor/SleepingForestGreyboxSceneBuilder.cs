using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WonderSquad.Editor
{
    /// <summary>
    /// One-time authoring utility for the Sprint003A formal greybox scene.
    /// It refuses to overwrite an existing scene.
    /// </summary>
    public static class SleepingForestGreyboxSceneBuilder
    {
        private const string ScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string PlayerPrefabPath =
            "Assets/WonderSquad/Prefabs/Player/Player.prefab";
        private const string CameraRigPrefabPath =
            "Assets/WonderSquad/Prefabs/Player/PF_PlayerCameraRig.prefab";
        private const string PromptPrefabPath =
            "Assets/WonderSquad/Prefabs/UI/PF_InteractionPrompt.prefab";
        private const string MaterialDirectory =
            "Assets/WonderSquad/Art/Materials/Greybox";
        private const string PlayerSpawnerScriptPath =
            "Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawner.cs";
        private const string PlayerSpawnPointScriptPath =
            "Assets/WonderSquad/Runtime/Player/Spawning/PlayerSpawnPoint.cs";
        private const string CameraBinderScriptPath =
            "Assets/WonderSquad/Runtime/Camera/CameraTargetBinder.cs";
        private const string PromptPresenterScriptPath =
            "Assets/WonderSquad/Runtime/UI/Interaction/InteractionPromptPresenter.cs";
        private const string FallRecoveryScriptPath =
            "Assets/WonderSquad/Runtime/Level/SleepingForest/Greybox/SleepingForestFallRecovery.cs";
        private const float FallYThreshold = -8f;
        private const float MainRouteWidth = 6f;
        private const float RecoveryRouteWidth = 6f;
        private const float RootBridgeWidth = 7f;
        private const float JunctionPlatformSize = 8f;

        [MenuItem("Wonder Squad/Sprint003A/Create Missing Sleeping Forest Greybox")]
        public static void CreateFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Create Sleeping Forest Greybox",
                    "Creates the Sprint003A formal greybox only when it is missing. " +
                    "An existing scene is never overwritten by this command.",
                    "Create",
                    "Cancel"))
            {
                return;
            }

            if (!TryPrepareMenuSceneState(false, out var reusableScene))
            {
                return;
            }

            CreateMissingScene(reusableScene);
        }

        [MenuItem("Wonder Squad/Sprint003A/Rebuild Sleeping Forest Greybox (Development Only)")]
        public static void RebuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild Sleeping Forest Greybox",
                    "This development-only command replaces the existing SleepingForest " +
                    "greybox scene with the current Builder layout. Continue?",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            if (!TryPrepareMenuSceneState(true, out var reusableScene))
            {
                return;
            }

            CreateScene(true, reusableScene);
        }

        public static void CreateFromCommandLine()
        {
            CreateMissingScene(GetReusableUntitledSceneOrThrow());
        }

        private static void CreateMissingScene(Scene reusableScene)
        {
            if (SceneAlreadyExists())
            {
                Debug.Log(
                    "[WonderSquad] SleepingForest greybox already exists; " +
                    "Create Missing skipped without changes.");
                return;
            }

            CreateScene(false, reusableScene);
        }

        private static void CreateScene(
            bool shouldOverwriteExistingScene,
            Scene reusableScene)
        {
            if (SceneAlreadyExists() && !shouldOverwriteExistingScene)
            {
                throw new InvalidOperationException(
                    "SleepingForest scene already exists and will not be overwritten.");
            }

            EnsureAssetFolder(Path.GetDirectoryName(ScenePath)?.Replace('\\', '/') ?? string.Empty);
            EnsureAssetFolder(MaterialDirectory);

            var materials = new GreyboxMaterials(
                GetOrCreateMaterial("M_SF_Greybox_Ground", new Color(0.17f, 0.30f, 0.20f)),
                GetOrCreateMaterial("M_SF_Greybox_Route", new Color(0.38f, 0.31f, 0.18f)),
                GetOrCreateMaterial("M_SF_Greybox_Beacon", new Color(0.18f, 0.72f, 0.78f)),
                GetOrCreateMaterial("M_SF_Greybox_Bridge", new Color(0.34f, 0.20f, 0.10f)),
                GetOrCreateMaterial("M_SF_Greybox_End", new Color(0.88f, 0.72f, 0.24f)),
                GetOrCreateMaterial("M_SF_Greybox_Recovery", new Color(0.46f, 0.28f, 0.48f)),
                GetOrCreateMaterial("M_SF_Greybox_Blocked", new Color(0.36f, 0.14f, 0.14f)));

            var previousActiveScene = SceneManager.GetActiveScene();
            var scene = GetBuildScene(
                shouldOverwriteExistingScene,
                reusableScene,
                out var didCreateAdditiveScene);
            SceneManager.SetActiveScene(scene);

            try
            {
                if (shouldOverwriteExistingScene && scene.path == ScenePath)
                {
                    ClearScene(scene);
                }

                var root = CreateEmpty("SleepingForestRoot", null, scene);
                var playerSetup = CreateEmpty("PlayerSetup", root.transform, scene);
                var presentation = CreateEmpty("Presentation", root.transform, scene);
                var environment = CreateEmpty("Environment", root.transform, scene);
                var landmarks = CreateEmpty("Landmarks", root.transform, scene);
                var gameplay = CreateEmpty("Gameplay", root.transform, scene);
                var lighting = CreateEmpty("Lighting", root.transform, scene);

                CreatePlayerSetup(playerSetup.transform, presentation.transform, scene);
                CreateEnvironment(environment.transform, landmarks.transform, gameplay.transform, materials, scene);
                CreateLighting(lighting.transform, scene);

                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                {
                    throw new IOException(
                        "Failed to save SleepingForest greybox scene.");
                }
            }
            catch
            {
                if (didCreateAdditiveScene && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                throw;
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                shouldOverwriteExistingScene
                    ? "[WonderSquad] Rebuilt SleepingForest Sprint003A greybox scene."
                    : "[WonderSquad] Created SleepingForest Sprint003A greybox scene.");
        }

        private static bool SceneAlreadyExists()
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null ||
                File.Exists(Path.GetFullPath(ScenePath));
        }

        private static bool TryPrepareMenuSceneState(
            bool shouldOverwriteExistingScene,
            out Scene reusableScene)
        {
            reusableScene = default;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            if (shouldOverwriteExistingScene)
            {
                var loadedSleepingForest =
                    SceneManager.GetSceneByPath(ScenePath);
                if (loadedSleepingForest.IsValid() && loadedSleepingForest.isDirty)
                {
                    EditorUtility.DisplayDialog(
                        "Sleeping Forest Rebuild Cancelled",
                        "The loaded SleepingForest scene still has unsaved changes. " +
                        "Save or revert those changes explicitly before rebuilding.",
                        "OK");
                    return false;
                }
            }

            try
            {
                reusableScene = GetReusableUntitledSceneOrThrow();
                return true;
            }
            catch (InvalidOperationException exception)
            {
                EditorUtility.DisplayDialog(
                    "Sleeping Forest Builder Cancelled",
                    exception.Message,
                    "OK");
                return false;
            }
        }

        private static Scene GetReusableUntitledSceneOrThrow()
        {
            var reusableScene = default(Scene);
            for (var sceneIndex = 0;
                 sceneIndex < SceneManager.sceneCount;
                 sceneIndex++)
            {
                var openScene = SceneManager.GetSceneAt(sceneIndex);
                if (!string.IsNullOrEmpty(openScene.path))
                {
                    continue;
                }

                if (openScene.isDirty || openScene.rootCount > 0)
                {
                    throw new InvalidOperationException(
                        "An untitled scene still contains unsaved content. " +
                        "Save or close it explicitly, then run the Builder again. " +
                        "No scene or asset was changed.");
                }

                if (!reusableScene.IsValid())
                {
                    reusableScene = openScene;
                }
            }

            return reusableScene;
        }

        private static Scene GetBuildScene(
            bool shouldOverwriteExistingScene,
            Scene reusableScene,
            out bool didCreateAdditiveScene)
        {
            didCreateAdditiveScene = false;
            if (shouldOverwriteExistingScene)
            {
                var loadedSleepingForest =
                    SceneManager.GetSceneByPath(ScenePath);
                if (loadedSleepingForest.IsValid() && loadedSleepingForest.isLoaded)
                {
                    return loadedSleepingForest;
                }
            }

            if (reusableScene.IsValid() && reusableScene.isLoaded)
            {
                return reusableScene;
            }

            didCreateAdditiveScene = true;
            return EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
        }

        private static void ClearScene(Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
            }
        }

        private static void CreatePlayerSetup(
            Transform playerSetup,
            Transform presentation,
            Scene scene)
        {
            var spawnObject = CreateEmpty("PlayerSpawnPoint", playerSetup, scene);
            spawnObject.transform.position = new Vector3(0f, 0.1f, -48f);
            var spawnPoint = AddComponentFromScript(
                spawnObject,
                PlayerSpawnPointScriptPath);

            var spawnerObject = CreateEmpty("PlayerSpawner", playerSetup, scene);
            var playerSpawner = AddComponentFromScript(
                spawnerObject,
                PlayerSpawnerScriptPath);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                throw new InvalidOperationException("Player prefab is missing.");
            }

            SetObjectReference(playerSpawner, "playerPrefab", playerPrefab);
            SetObjectReference(playerSpawner, "spawnPoint", spawnPoint);

            var recoveryPoint = CreateEmpty("RecoveryPoint", playerSetup, scene);
            recoveryPoint.transform.position = new Vector3(0f, 0.1f, -46f);
            var fallRecoveryObject = CreateEmpty(
                "SleepingForestFallRecovery",
                playerSetup,
                scene);
            var fallRecovery = AddComponentFromScript(
                fallRecoveryObject,
                FallRecoveryScriptPath);
            SetObjectReference(fallRecovery, "playerSpawner", playerSpawner);
            SetObjectReference(fallRecovery, "recoveryPoint", recoveryPoint.transform);
            SetFloat(fallRecovery, "fallYThreshold", FallYThreshold);

            var cameraRig = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(CameraRigPrefabPath),
                scene) as GameObject;
            if (cameraRig == null)
            {
                throw new InvalidOperationException("Player camera rig prefab is missing.");
            }

            cameraRig.name = "PF_PlayerCameraRig";
            cameraRig.transform.SetParent(presentation, false);
            SetObjectReference(
                GetComponentFromScript(cameraRig, CameraBinderScriptPath),
                "playerSpawner",
                playerSpawner);

            var promptCanvas = PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(PromptPrefabPath),
                scene) as GameObject;
            if (promptCanvas == null)
            {
                throw new InvalidOperationException("Interaction prompt prefab is missing.");
            }

            promptCanvas.name = "InteractionPromptCanvas";
            promptCanvas.transform.SetParent(presentation, false);
            SetObjectReference(
                GetComponentFromScript(promptCanvas, PromptPresenterScriptPath),
                "playerSpawner",
                playerSpawner);
        }

        private static void CreateEnvironment(
            Transform environment,
            Transform landmarks,
            Transform gameplay,
            GreyboxMaterials materials,
            Scene scene)
        {
            var terrain = CreateEmpty("Terrain", environment, scene).transform;
            var mainRoute = CreateEmpty("MainRoute", environment, scene).transform;
            var advantageRoute = CreateEmpty("AdvantageRouteReserved", environment, scene).transform;
            var recoveryRoute = CreateEmpty("RecoveryRoute", environment, scene).transform;
            var mainRouteJunctions = CreateEmpty(
                "JunctionPlatforms",
                mainRoute,
                scene).transform;
            var recoveryRouteJunctions = CreateEmpty(
                "JunctionPlatforms",
                recoveryRoute,
                scene).transform;
            var boundaries = CreateEmpty("Boundaries", environment, scene).transform;
            var beaconLandmarks = CreateEmpty("BeaconLandmarks", landmarks, scene).transform;
            var futureGameplayAnchors = CreateEmpty("FutureGameplayAnchors", gameplay, scene).transform;

            CreateCube(
                "SpawnAreaGround",
                terrain,
                new Vector3(0f, -0.5f, -48f),
                new Vector3(18f, 1f, 18f),
                materials.Ground,
                scene);
            CreateCube(
                "ForestEntranceGround",
                terrain,
                new Vector3(0f, -0.5f, -31f),
                new Vector3(10f, 1f, 16f),
                materials.Ground,
                scene);
            CreateCube(
                "ObservationAreaGround",
                terrain,
                new Vector3(0f, -0.5f, -15.5f),
                new Vector3(26f, 1f, 15f),
                materials.Ground,
                scene);

            CreateRouteConnection(
                "MainRoute_ToBeaconA",
                mainRoute,
                new Vector3(-10f, -0.5f, -8f),
                new Vector3(-10f, -0.5f, -4f),
                MainRouteWidth,
                materials.Route,
                scene);
            CreateCube(
                "MainRouteJunction_Northwest",
                mainRouteJunctions,
                new Vector3(-12f, -0.5f, 12f),
                new Vector3(JunctionPlatformSize, 1f, JunctionPlatformSize),
                materials.Route,
                scene);
            CreateRouteConnection(
                "MainRoute_BeaconAToNorthwestJunction",
                mainRoute,
                new Vector3(-12f, -0.5f, 4f),
                new Vector3(-12f, -0.5f, 8f),
                MainRouteWidth,
                materials.Route,
                scene);
            CreateRouteConnection(
                "MainRoute_NorthwestJunctionToBeaconB",
                mainRoute,
                new Vector3(-8f, -0.5f, 12f),
                new Vector3(-4f, -0.5f, 12f),
                MainRouteWidth,
                materials.Route,
                scene);
            CreateCube(
                "RootBridgeEntranceJunction",
                mainRouteJunctions,
                new Vector3(0f, -0.5f, 23f),
                new Vector3(JunctionPlatformSize, 1f, JunctionPlatformSize),
                materials.Bridge,
                scene);
            CreateRouteConnection(
                "MainRoute_BeaconBToBridge",
                mainRoute,
                new Vector3(0f, -0.5f, 16f),
                new Vector3(0f, -0.5f, 19f),
                MainRouteWidth,
                materials.Route,
                scene);
            CreateCube(
                "RootBridgeTemporaryCrossing",
                mainRoute,
                new Vector3(0f, -0.5f, 35f),
                new Vector3(RootBridgeWidth, 1f, 16f),
                materials.Bridge,
                scene);
            CreateCube(
                "SliceEndGround",
                terrain,
                new Vector3(0f, -0.5f, 50f),
                new Vector3(20f, 1f, 14f),
                materials.Ground,
                scene);

            CreateCube(
                "RecoveryRouteEntryJunction",
                recoveryRouteJunctions,
                new Vector3(9f, -0.5f, -4f),
                new Vector3(JunctionPlatformSize, 1f, JunctionPlatformSize),
                materials.Recovery,
                scene);
            CreateCube(
                "RecoveryLeafPad",
                recoveryRouteJunctions,
                new Vector3(23f, -0.5f, -4f),
                new Vector3(JunctionPlatformSize, 1f, JunctionPlatformSize),
                materials.Recovery,
                scene);
            CreateCube(
                "RecoveryRouteReturnJunction",
                recoveryRouteJunctions,
                new Vector3(23f, -0.5f, 12f),
                new Vector3(JunctionPlatformSize, 1f, JunctionPlatformSize),
                materials.Recovery,
                scene);
            CreateRouteConnection(
                "RecoveryRoute_OuterLoop",
                recoveryRoute,
                new Vector3(13f, -0.5f, -4f),
                new Vector3(19f, -0.5f, -4f),
                RecoveryRouteWidth,
                materials.Recovery,
                scene);
            CreateRouteConnection(
                "RecoveryRoute_ReturnNorth",
                recoveryRoute,
                new Vector3(23f, -0.5f, 0f),
                new Vector3(23f, -0.5f, 8f),
                RecoveryRouteWidth,
                materials.Recovery,
                scene);
            CreateRouteConnection(
                "RecoveryRoute_ReturnLoop",
                recoveryRoute,
                new Vector3(19f, -0.5f, 12f),
                new Vector3(4f, -0.5f, 12f),
                RecoveryRouteWidth,
                materials.Recovery,
                scene);

            CreateCube(
                "AdvantageRouteEntryPlatform",
                advantageRoute,
                new Vector3(-15.5f, -0.5f, -11f),
                new Vector3(5f, 1f, 6f),
                materials.Route,
                scene);
            CreateCube(
                "AdvantageRouteHighPlatform",
                advantageRoute,
                new Vector3(-23f, 2f, 0f),
                new Vector3(8f, 1f, 28f),
                materials.Route,
                scene);
            CreateCube(
                "AdvantageRouteVisibleBlocker",
                advantageRoute,
                new Vector3(-17.75f, 1.5f, -11f),
                new Vector3(0.5f, 3f, 6f),
                materials.Blocked,
                scene);
            CreateCube(
                "AdvantageRouteSupport",
                advantageRoute,
                new Vector3(-23f, 0.5f, 0f),
                new Vector3(10f, 2f, 30f),
                materials.Blocked,
                scene);

            CreateBeacon(
                beaconLandmarks,
                futureGameplayAnchors,
                "BeaconA_Reserved",
                new Vector3(-12f, 0f, 0f),
                materials.Beacon,
                scene);
            CreateBeacon(
                beaconLandmarks,
                futureGameplayAnchors,
                "BeaconB_Reserved",
                new Vector3(0f, 0f, 12f),
                materials.Beacon,
                scene);
            CreateEndLandmark(landmarks, new Vector3(0f, 0f, 50f), materials.End, scene);

            CreateBoundary(
                boundaries,
                "North",
                new Vector3(0f, 1f, 58f),
                new Vector3(64f, 2f, 1f),
                materials.Blocked,
                scene);
            CreateBoundary(
                boundaries,
                "South",
                new Vector3(0f, 1f, -58f),
                new Vector3(64f, 2f, 1f),
                materials.Blocked,
                scene);
            CreateBoundary(
                boundaries,
                "East",
                new Vector3(32f, 1f, 0f),
                new Vector3(1f, 2f, 116f),
                materials.Blocked,
                scene);
            CreateBoundary(
                boundaries,
                "West",
                new Vector3(-32f, 1f, 0f),
                new Vector3(1f, 2f, 116f),
                materials.Blocked,
                scene);
            CreateBoundary(
                boundaries,
                "RootBridgeWestGuardrail",
                new Vector3(-3.75f, 0.75f, 35f),
                new Vector3(0.5f, 1.5f, 16f),
                materials.Blocked,
                scene);
            CreateBoundary(
                boundaries,
                "RootBridgeEastGuardrail",
                new Vector3(3.75f, 0.75f, 35f),
                new Vector3(0.5f, 1.5f, 16f),
                materials.Blocked,
                scene);
        }

        private static void CreateLighting(Transform lighting, Scene scene)
        {
            var directionalLight = new GameObject("DirectionalLight", typeof(Light));
            SceneManager.MoveGameObjectToScene(directionalLight, scene);
            directionalLight.transform.SetParent(lighting, false);
            directionalLight.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            var light = directionalLight.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.82f, 0.92f, 0.78f);

            CreatePointLight(lighting, "BeaconAreaLight", new Vector3(0f, 6f, 5f), new Color(0.25f, 0.78f, 0.82f), 4f, 11f, scene);
            CreatePointLight(lighting, "EndAreaLight", new Vector3(0f, 7f, 50f), new Color(1f, 0.78f, 0.30f), 4f, 12f, scene);
            CreatePointLight(lighting, "EntranceLight", new Vector3(0f, 4f, -31f), new Color(0.42f, 0.78f, 0.42f), 2.5f, 8f, scene);
        }

        private static void CreateBeacon(
            Transform landmarks,
            Transform anchors,
            string beaconName,
            Vector3 position,
            Material material,
            Scene scene)
        {
            var beacon = CreatePrimitive(
                PrimitiveType.Cylinder,
                beaconName,
                landmarks,
                position + new Vector3(0f, 3.5f, 0f),
                new Vector3(2.2f, 3.5f, 2.2f),
                material,
                scene);
            var beaconCollider = beacon.GetComponent<Collider>();
            if (beaconCollider != null)
            {
                beaconCollider.enabled = false;
            }

            CreateCube(
                beaconName + "_Base",
                landmarks,
                position + new Vector3(0f, -0.5f, 0f),
                new Vector3(JunctionPlatformSize, 1f, JunctionPlatformSize),
                material,
                scene);
            var anchor = CreateEmpty(beaconName + "_InteractionAnchor_Reserved", anchors, scene);
            anchor.transform.position = position + new Vector3(0f, 0f, -3f);
        }

        private static void CreateEndLandmark(
            Transform landmarks,
            Vector3 position,
            Material material,
            Scene scene)
        {
            CreatePrimitive(
                PrimitiveType.Cylinder,
                "SliceEndLandmark_Tower",
                landmarks,
                position + new Vector3(0f, 5f, 0f),
                new Vector3(5f, 5f, 5f),
                material,
                scene);
            CreateCube(
                "SliceEndLandmark_Arch",
                landmarks,
                position + new Vector3(0f, 5f, -3.5f),
                new Vector3(10f, 1.5f, 1.5f),
                material,
                scene);
        }

        private static void CreateRouteSegment(
            string name,
            Transform parent,
            Vector3 position,
            float length,
            float yawDegrees,
            float width,
            Material material,
            Scene scene)
        {
            var route = CreateCube(
                name,
                parent,
                position,
                new Vector3(width, 1f, length),
                material,
                scene);
            route.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        private static void CreateRouteConnection(
            string name,
            Transform parent,
            Vector3 start,
            Vector3 end,
            float width,
            Material material,
            Scene scene)
        {
            var direction = end - start;
            var horizontalDirection = new Vector2(direction.x, direction.z);
            var length = horizontalDirection.magnitude;
            if (length <= Mathf.Epsilon)
            {
                throw new ArgumentException(
                    "A route connection requires two distinct points.",
                    nameof(end));
            }

            var center = (start + end) * 0.5f;
            var yawDegrees =
                Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            CreateRouteSegment(
                name,
                parent,
                center,
                length,
                yawDegrees,
                width,
                material,
                scene);
        }

        private static void CreateBoundary(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Scene scene)
        {
            CreateCube("Boundary_" + name, parent, position, scale, material, scene);
        }

        private static void CreatePointLight(
            Transform parent,
            string name,
            Vector3 position,
            Color color,
            float intensity,
            float range,
            Scene scene)
        {
            var lightObject = new GameObject(name, typeof(Light));
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Scene scene)
        {
            return CreatePrimitive(
                PrimitiveType.Cube,
                name,
                parent,
                position,
                scale,
                material,
                scene);
        }

        private static GameObject CreatePrimitive(
            PrimitiveType primitiveType,
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material,
            Scene scene)
        {
            var gameObject = GameObject.CreatePrimitive(primitiveType);
            gameObject.name = name;
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            return gameObject;
        }

        private static GameObject CreateEmpty(
            string name,
            Transform parent,
            Scene scene)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            if (parent != null)
            {
                gameObject.transform.SetParent(parent, false);
            }

            return gameObject;
        }

        private static Component AddComponentFromScript(
            GameObject gameObject,
            string scriptPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null || script.GetClass() == null)
            {
                throw new InvalidOperationException("Missing component script: " + scriptPath);
            }

            return gameObject.AddComponent(script.GetClass());
        }

        private static Component GetComponentFromScript(
            GameObject gameObject,
            string scriptPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
            if (script == null || script.GetClass() == null)
            {
                throw new InvalidOperationException("Missing component script: " + scriptPath);
            }

            var component = gameObject.GetComponentInChildren(script.GetClass(), true);
            if (component == null)
            {
                throw new InvalidOperationException("Prefab component is missing: " + scriptPath);
            }

            return component;
        }

        private static void SetObjectReference(
            Component component,
            string propertyName,
            UnityEngine.Object value)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized property '{propertyName}' on {component.GetType().Name}.");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(
            Component component,
            string propertyName,
            float value)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Missing serialized property '{propertyName}' on {component.GetType().Name}.");
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            var path = MaterialDirectory + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            }

            material = new Material(shader)
            {
                name = name
            };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException("An asset folder path is required.", nameof(assetPath));
            }

            var currentPath = "Assets";
            var pathParts = assetPath.Split('/');
            for (var index = 1; index < pathParts.Length; index++)
            {
                var nextPath = currentPath + "/" + pathParts[index];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[index]);
                }

                currentPath = nextPath;
            }
        }

        private readonly struct GreyboxMaterials
        {
            public GreyboxMaterials(
                Material ground,
                Material route,
                Material beacon,
                Material bridge,
                Material end,
                Material recovery,
                Material blocked)
            {
                Ground = ground;
                Route = route;
                Beacon = beacon;
                Bridge = bridge;
                End = end;
                Recovery = recovery;
                Blocked = blocked;
            }

            public Material Ground { get; }

            public Material Route { get; }

            public Material Beacon { get; }

            public Material Bridge { get; }

            public Material End { get; }

            public Material Recovery { get; }

            public Material Blocked { get; }
        }
    }
}
