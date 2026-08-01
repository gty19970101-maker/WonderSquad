using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using WonderSquad.Core.Configuration;

namespace WonderSquad.Editor
{
    public enum P0SceneCreationMode
    {
        CreateMissingOnly,
        RebuildExisting
    }

    public static class P0ProjectSetup
    {
        private const string AutomaticSetupSessionKey =
            "WonderSquad.P0.AutomaticSetupApplied";
        private const string UrpDirectory =
            "Assets/WonderSquad/Settings/URP";
        private const string RendererDataPath =
            UrpDirectory + "/P0_UniversalRenderer.asset";
        private const string PipelineAssetPath =
            UrpDirectory + "/P0_UniversalRenderPipeline.asset";
        private const string GeneratedGlobalSettingsPath =
            "Assets/UniversalRenderPipelineGlobalSettings.asset";
        private const string ProjectGlobalSettingsPath =
            UrpDirectory + "/UniversalRenderPipelineGlobalSettings.asset";

        public static P0SceneCreationMode AutomaticSceneCreationMode =>
            P0SceneCreationMode.CreateMissingOnly;

        [MenuItem("Wonder Squad/P0/Apply Project Setup (Safe)")]
        public static void ApplyFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Apply Wonder Squad P0 Setup",
                    "This assigns the P0 project settings and creates only missing scenes. Existing scenes will not be changed.",
                    "Apply",
                    "Cancel"))
            {
                return;
            }

            ApplyFinalProjectSetup();
        }

        [MenuItem("Wonder Squad/P0/Rebuild All P0 Scenes...")]
        public static void RebuildScenesFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild All Wonder Squad P0 Scenes?",
                    "This destructive operation replaces all four P0 scenes, including PlayerSandbox. Existing scene changes will be lost.",
                    "Rebuild Scenes",
                    "Cancel"))
            {
                return;
            }

            ApplyProjectSetup(P0SceneCreationMode.RebuildExisting);
        }

        [InitializeOnLoadMethod]
        private static void ScheduleAutomaticSetupIfRequired()
        {
            if (Application.isBatchMode ||
                SessionState.GetBool(AutomaticSetupSessionKey, false))
            {
                return;
            }

            EditorApplication.delayCall += ApplyAutomaticSetupIfRequired;
        }

        private static void ApplyAutomaticSetupIfRequired()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SessionState.SetBool(AutomaticSetupSessionKey, true);
            try
            {
                ApplyProjectSetup(AutomaticSceneCreationMode);
            }
            catch
            {
                SessionState.SetBool(AutomaticSetupSessionKey, false);
                throw;
            }
        }

        public static void ApplyFinalProjectSetup()
        {
            ApplyProjectSetup(P0SceneCreationMode.CreateMissingOnly);
        }

        public static bool EnsureP0SceneExists(
            string scenePath,
            string rootName)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                throw new ArgumentException(
                    "A scene asset path is required.",
                    nameof(scenePath));
            }

            if (string.IsNullOrWhiteSpace(rootName))
            {
                throw new ArgumentException(
                    "A scene root name is required.",
                    nameof(rootName));
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null ||
                File.Exists(Path.GetFullPath(scenePath)))
            {
                Debug.Log(
                    $"[WonderSquad] P0 scene already exists; skipped without changes: {scenePath}");
                return false;
            }

            BuildP0Scene(scenePath, rootName);
            Debug.Log($"[WonderSquad] Created missing P0 scene: {scenePath}");
            return true;
        }

        private static void ApplyProjectSetup(P0SceneCreationMode sceneMode)
        {
            var pipelineAsset = EnsureUrpAssets();
            AssignUrp(pipelineAsset);

            ConfigureP0Scene(
                ProjectConstants.BootstrapScenePath,
                "P0_BootstrapScene",
                sceneMode);
            ConfigureP0Scene(
                ProjectConstants.PlayerSandboxScenePath,
                "P0_PlayerSandbox_NoGameplay",
                sceneMode);
            ConfigureP0Scene(
                ProjectConstants.GameplaySandboxScenePath,
                "P0_GameplaySandbox_NoGameplay",
                sceneMode);
            ConfigureP0Scene(
                ProjectConstants.RecoverySandboxScenePath,
                "P0_RecoverySandbox_NoGameplay",
                sceneMode);

            EditorBuildSettings.scenes = new[]
            {
                BuildScene(ProjectConstants.BootstrapScenePath),
                BuildScene(ProjectConstants.PlayerSandboxScenePath),
                BuildScene(ProjectConstants.GameplaySandboxScenePath),
                BuildScene(ProjectConstants.RecoverySandboxScenePath)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var errors = P0ProjectValidator.CollectValidationErrors();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            Debug.Log(
                $"[WonderSquad] P0 project setup completed with scene mode: {sceneMode}.");
        }

        private static void ConfigureP0Scene(
            string scenePath,
            string rootName,
            P0SceneCreationMode sceneMode)
        {
            if (sceneMode == P0SceneCreationMode.RebuildExisting)
            {
                BuildP0Scene(scenePath, rootName);
                Debug.Log($"[WonderSquad] Explicitly rebuilt P0 scene: {scenePath}");
                return;
            }

            EnsureP0SceneExists(scenePath, rootName);
        }

        private static UniversalRenderPipelineAsset EnsureUrpAssets()
        {
            EnsureAssetFolder(UrpDirectory);

            if (AssetDatabase.LoadMainAssetAtPath(
                    GeneratedGlobalSettingsPath) != null &&
                AssetDatabase.LoadMainAssetAtPath(
                    ProjectGlobalSettingsPath) == null)
            {
                var moveError = AssetDatabase.MoveAsset(
                    GeneratedGlobalSettingsPath,
                    ProjectGlobalSettingsPath);
                if (!string.IsNullOrEmpty(moveError))
                {
                    throw new IOException(moveError);
                }
            }

            var rendererData =
                AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                    RendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);
            }

            var pipelineAsset =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                    PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            }

            return pipelineAsset;
        }

        private static void AssignUrp(RenderPipelineAsset pipelineAsset)
        {
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            var originalQualityLevel = QualitySettings.GetQualityLevel();
            for (var index = 0; index < QualitySettings.names.Length; index++)
            {
                QualitySettings.SetQualityLevel(index, false);
                QualitySettings.renderPipeline = pipelineAsset;
            }

            QualitySettings.SetQualityLevel(originalQualityLevel, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }

        private static void BuildP0Scene(string scenePath, string rootName)
        {
            var scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);

            var root = new GameObject(rootName);

            var cameraMount = CreateEmpty("CameraMount", root.transform);
            cameraMount.transform.position = new Vector3(0f, 7f, -10f);
            cameraMount.transform.LookAt(Vector3.zero);

            var cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(cameraMount.transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.08f, 0.07f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var lightObject = new GameObject(
                "Directional Light",
                typeof(Light));
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground_Placeholder";
            ground.transform.SetParent(root.transform, false);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(12f, 1f, 12f);

            var centerMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerMarker.name = "P0_VisibleMarker";
            centerMarker.transform.SetParent(root.transform, false);
            centerMarker.transform.position = new Vector3(0f, 0.75f, 0f);
            centerMarker.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            var spawnPoint = CreateEmpty("SpawnPoint", root.transform);
            spawnPoint.transform.position = new Vector3(0f, 0.1f, -2f);

            var boundaryRoot = CreateEmpty("BoundaryRoot", root.transform);
            CreateBoundary(boundaryRoot.transform, "North", new Vector3(0f, 1f, 6f), new Vector3(12f, 2f, 0.25f));
            CreateBoundary(boundaryRoot.transform, "South", new Vector3(0f, 1f, -6f), new Vector3(12f, 2f, 0.25f));
            CreateBoundary(boundaryRoot.transform, "East", new Vector3(6f, 1f, 0f), new Vector3(0.25f, 2f, 12f));
            CreateBoundary(boundaryRoot.transform, "West", new Vector3(-6f, 1f, 0f), new Vector3(0.25f, 2f, 12f));

            var debugCanvas = new GameObject(
                "DebugCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            debugCanvas.transform.SetParent(root.transform, false);
            debugCanvas.GetComponent<Canvas>().renderMode =
                RenderMode.ScreenSpaceOverlay;

            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new IOException($"Failed to save P0 scene: {scenePath}");
            }

            if (!EditorSceneManager.CloseScene(scene, true))
            {
                throw new InvalidOperationException(
                    $"Failed to close generated P0 scene: {scenePath}");
            }
        }

        private static GameObject CreateEmpty(string name, Transform parent)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void CreateBoundary(
            Transform parent,
            string suffix,
            Vector3 position,
            Vector3 scale)
        {
            var boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = "Boundary_" + suffix;
            boundary.transform.SetParent(parent, false);
            boundary.transform.position = position;
            boundary.transform.localScale = scale;
        }

        private static EditorBuildSettingsScene BuildScene(string scenePath)
        {
            return new EditorBuildSettingsScene(scenePath, true);
        }

        private static void EnsureAssetFolder(string assetPath)
        {
            var current = "Assets";
            var parts = assetPath.Split('/');
            for (var index = 1; index < parts.Length; index++)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
