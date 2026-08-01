using System.IO;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using WonderSquad.Player.Camera;
using WonderSquad.Presentation.Camera;

namespace WonderSquad.Tests.EditMode
{
    public sealed class CameraFollowEditModeTests
    {
        private const string CameraSettingsPath =
            "Assets/WonderSquad/ScriptableObjects/Configuration/CameraFollowSettings.asset";
        private const string PlayerPrefabPath =
            "Assets/WonderSquad/Prefabs/Player/Player.prefab";
        private const string CameraAssemblyPath =
            "Assets/WonderSquad/Runtime/Camera/WonderSquad.Camera.asmdef";
        private const string PlayerAssemblyPath =
            "Assets/WonderSquad/Runtime/Player/WonderSquad.Player.asmdef";
        private const string PackageManifestPath =
            "Packages/manifest.json";
        private const string CinemachinePackageVersion = "3.1.7";

        [Test]
        public void CameraFollowSettings_DefaultAssetIsValid()
        {
            var settings = LoadSettings();

            Assert.That(settings.IsValid, Is.True);
            Assert.That(settings.FollowOffset.sqrMagnitude, Is.GreaterThan(0f));
            Assert.That(settings.FixedEulerAngles.x, Is.InRange(1f, 89f));
            Assert.That(settings.FixedEulerAngles.z, Is.EqualTo(0f));
        }

        [Test]
        public void CameraFollowSettings_OffsetAndDampingAreWithinValidRanges()
        {
            var settings = LoadSettings();

            Assert.That(IsFinite(settings.TargetLocalOffset), Is.True);
            Assert.That(IsFinite(settings.FollowOffset), Is.True);
            Assert.That(
                CameraFollowSettings.IsValidDamping(
                    settings.PositionDamping),
                Is.True);
            Assert.That(
                CameraFollowSettings.IsValidDamping(
                    new Vector3(-1f, 0f, 0f)),
                Is.False);
            Assert.That(
                CameraFollowSettings.IsValidDamping(
                    new Vector3(float.NaN, 0f, 0f)),
                Is.False);
        }

        [Test]
        public void PlayerPrefab_HasValidIndependentCameraTarget()
        {
            var playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Assert.That(playerPrefab, Is.Not.Null);
            var target =
                playerPrefab.GetComponentInChildren<PlayerCameraTarget>(true);
            Assert.That(target, Is.Not.Null);
            Assert.That(target.IsConfigurationValid, Is.True);
            Assert.That(target.transform, Is.Not.SameAs(playerPrefab.transform));
            Assert.That(
                playerPrefab.GetComponentInChildren<UnityEngine.Camera>(true),
                Is.Null);
        }

        [Test]
        public void CameraAssembly_DependencyDirectionMatchesArchitecture()
        {
            var cameraAssembly = LoadAssemblyDefinition(CameraAssemblyPath);
            var playerAssembly = LoadAssemblyDefinition(PlayerAssemblyPath);

            Assert.That(
                cameraAssembly.references,
                Does.Contain("WonderSquad.Core"));
            Assert.That(
                cameraAssembly.references,
                Does.Contain("WonderSquad.Player"));
            Assert.That(
                cameraAssembly.references,
                Does.Contain("Unity.Cinemachine"));
            Assert.That(
                playerAssembly.references,
                Does.Not.Contain("WonderSquad.Camera"));
        }

        [Test]
        public void CinemachinePackage_IsExactlyRequiredVersion()
        {
            var packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(CinemachineCamera).Assembly);
            var manifest = File.ReadAllText(PackageManifestPath);

            Assert.That(packageInfo, Is.Not.Null);
            Assert.That(
                packageInfo.version,
                Is.EqualTo(CinemachinePackageVersion));
            StringAssert.Contains(
                "\"com.unity.cinemachine\": \"3.1.7\"",
                manifest);
        }

        private static CameraFollowSettings LoadSettings()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<CameraFollowSettings>(
                    CameraSettingsPath);

            Assert.That(settings, Is.Not.Null);
            return settings;
        }

        private static AssemblyDefinitionData LoadAssemblyDefinition(
            string path)
        {
            var json = File.ReadAllText(path);
            var definition =
                JsonUtility.FromJson<AssemblyDefinitionData>(json);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.references, Is.Not.Null);
            return definition;
        }

        private static bool IsFinite(Vector3 value)
        {
            return
                !float.IsNaN(value.x) &&
                !float.IsInfinity(value.x) &&
                !float.IsNaN(value.y) &&
                !float.IsInfinity(value.y) &&
                !float.IsNaN(value.z) &&
                !float.IsInfinity(value.z);
        }

        [System.Serializable]
        private sealed class AssemblyDefinitionData
        {
            public string[] references;
        }
    }
}
