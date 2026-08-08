using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WonderSquad.Core.Configuration;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Player.Movement;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Tests.PlayMode
{
    public sealed class InteractionDetectionPlayModeTests :
        InputTestFixture
    {
        private const string InteractableLayerName = "Interactable";
        private const int MovementObservationFrameCount = 3;
        private const int AllocationWarmupCount = 32;
        private const int AllocationObservationCount = 128;

        [UnityTest]
        public IEnumerator NearbyTarget_IsDetectedWithoutChangingTargetState()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var detector = FindDetector();
            DisableSceneTargets();
            var target = CreateTarget(
                "interaction.test.nearby",
                detector.DetectionOrigin.position + Vector3.forward);
            Physics.SyncTransforms();

            Assert.That(detector.RefreshDetection(), Is.True);
            Assert.That(detector.CurrentTarget, Is.SameAs(target));
            Assert.That(target.IsDetectionEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator TargetOutsideRadius_IsNotDetected()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var detector = FindDetector();
            DisableSceneTargets();
            CreateTarget(
                "interaction.test.far",
                detector.DetectionOrigin.position +
                Vector3.forward *
                (detector.Settings.DetectionRadius + 1f));
            Physics.SyncTransforms();

            Assert.That(detector.RefreshDetection(), Is.False);
            Assert.That(detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator TargetOnUnmatchedLayer_IsNotDetected()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var detector = FindDetector();
            DisableSceneTargets();
            CreateTarget(
                "interaction.test.layer_mismatch",
                detector.DetectionOrigin.position + Vector3.forward,
                0);
            Physics.SyncTransforms();

            Assert.That(detector.RefreshDetection(), Is.False);
            Assert.That(detector.CurrentTarget, Is.Null);
        }

        [UnityTest]
        public IEnumerator OccludedTarget_IsNotDetectedAndRecoversWhenClear()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var detector = FindDetector();
            DisableSceneTargets();
            var originPosition = detector.DetectionOrigin.position;
            var targetPosition =
                originPosition + Vector3.right * 2f;
            var target =
                CreateTarget(
                    "interaction.test.occluded",
                    targetPosition);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "InteractionOcclusionTestWall";
            wall.layer = 0;
            wall.transform.position =
                Vector3.Lerp(originPosition, targetPosition, 0.5f);
            wall.transform.localScale =
                new Vector3(1f, 2f, 0.25f);
            Physics.SyncTransforms();

            Assert.That(detector.RefreshDetection(), Is.False);
            Assert.That(detector.CurrentTarget, Is.Null);

            wall.SetActive(false);
            Physics.SyncTransforms();

            Assert.That(detector.RefreshDetection(), Is.True);
            Assert.That(detector.CurrentTarget, Is.SameAs(target));
        }

        [UnityTest]
        public IEnumerator Detection_DoesNotInterfereWithPlayerMovement()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return LoadPlayerSandbox();
            yield return null;

            var spawner = FindSpawner();
            var detector = FindDetector();
            var movement =
                spawner.SpawnedPlayer.GetComponent<PlayerMovement>();
            var initialPosition =
                spawner.SpawnedPlayer.transform.position;

            Assert.That(detector.HasValidConfiguration, Is.True);
            Assert.That(movement, Is.Not.Null);
            Assert.That(movement.HasValidConfiguration, Is.True);

            Press(keyboard.dKey);
            yield return ObserveFrames(MovementObservationFrameCount);
            Release(keyboard.dKey);
            yield return null;

            Assert.That(
                spawner.SpawnedPlayer.transform.position.x,
                Is.GreaterThan(initialPosition.x));
            Assert.That(
                spawner.SpawnedPlayer.GetComponent<Rigidbody>(),
                Is.Null);
        }

        [UnityTest]
        public IEnumerator StableDetection_DoesNotAllocateManagedMemory()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var detector = FindDetector();
            DisableSceneTargets();
            CreateTarget(
                "interaction.test.allocation",
                detector.DetectionOrigin.position + Vector3.forward);
            Physics.SyncTransforms();

            for (var index = 0;
                 index < AllocationWarmupCount;
                 index++)
            {
                detector.RefreshDetection();
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            var allocatedBytesBefore =
                System.GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0;
                 index < AllocationObservationCount;
                 index++)
            {
                detector.RefreshDetection();
            }

            var allocatedBytesAfter =
                System.GC.GetAllocatedBytesForCurrentThread();

            Assert.That(
                allocatedBytesAfter - allocatedBytesBefore,
                Is.EqualTo(0L));
            Assert.That(detector.HasCurrentTarget, Is.True);
        }

        [UnityTest]
        public IEnumerator DisabledDetector_ClearsAndResumesDetection()
        {
            yield return LoadPlayerSandbox();
            yield return null;

            var detector = FindDetector();
            DisableSceneTargets();
            CreateTarget(
                "interaction.test.lifecycle",
                detector.DetectionOrigin.position + Vector3.forward);
            Physics.SyncTransforms();

            Assert.That(detector.RefreshDetection(), Is.True);
            detector.enabled = false;

            Assert.That(detector.CurrentTarget, Is.Null);
            detector.enabled = true;
            Assert.That(detector.RefreshDetection(), Is.True);
        }

        private static IEnumerator LoadPlayerSandbox()
        {
            var loadOperation = SceneManager.LoadSceneAsync(
                ProjectConstants.PlayerSandboxScenePath,
                LoadSceneMode.Single);

            Assert.That(loadOperation, Is.Not.Null);
            while (!loadOperation.isDone)
            {
                yield return null;
            }
        }

        private static IEnumerator ObserveFrames(int frameCount)
        {
            for (var frame = 0; frame < frameCount; frame++)
            {
                yield return null;
            }
        }

        private static PlayerSpawner FindSpawner()
        {
            var spawners = Object.FindObjectsByType<PlayerSpawner>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Assert.That(spawners, Has.Length.EqualTo(1));
            Assert.That(spawners[0].SpawnedPlayer, Is.Not.Null);
            return spawners[0];
        }

        private static InteractionDetector FindDetector()
        {
            var spawner = FindSpawner();
            var detector =
                spawner.SpawnedPlayer.GetComponent<InteractionDetector>();

            Assert.That(detector, Is.Not.Null);
            Assert.That(detector.HasValidConfiguration, Is.True);
            return detector;
        }

        private static void DisableSceneTargets()
        {
            var targets = Object.FindObjectsByType<InteractionTarget>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var target in targets)
            {
                target.enabled = false;
            }

            var probes =
                Object.FindObjectsByType<InteractionExecutionProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            foreach (var probe in probes)
            {
                probe.enabled = false;
            }
        }

        private static InteractionTarget CreateTarget(
            string targetId,
            Vector3 position,
            int layer = -1)
        {
            var targetObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            targetObject.name = targetId;
            targetObject.transform.position = position;
            targetObject.layer =
                layer >= 0
                    ? layer
                    : LayerMask.NameToLayer(InteractableLayerName);

            var target =
                targetObject.AddComponent<InteractionTarget>();
            Assert.That(
                target.Configure(
                    targetId,
                    targetObject.transform,
                    true,
                    0),
                Is.True);
            return target;
        }
    }
}
