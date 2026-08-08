using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using WonderSquad.Core.Configuration;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Core.Input;
using WonderSquad.Interaction.Detection;
using WonderSquad.Interaction.Diagnostics;
using WonderSquad.Interaction.Execution;
using WonderSquad.Player.Identity;
using WonderSquad.Player.Input;

namespace WonderSquad.Tests.EditMode
{
    public sealed class InteractionExecutionEditModeTests
    {
        private const string PlayerPrefabPath =
            "Assets/WonderSquad/Prefabs/Player/Player.prefab";
        private const string InteractionSettingsPath =
            "Assets/WonderSquad/ScriptableObjects/Configuration/InteractionSettings.asset";
        private const string ExecutorSourcePath =
            "Assets/WonderSquad/Runtime/Interaction/Execution/InteractionExecutor.cs";

        [Test]
        public void IInteractable_RemainsDetectionOnly()
        {
            var methodNames = typeof(IInteractable)
                .GetMethods()
                .Select(method => method.Name)
                .ToArray();

            Assert.That(methodNames, Does.Not.Contain("Execute"));
            Assert.That(methodNames, Does.Not.Contain("Interact"));
            Assert.That(
                typeof(IExecutableInteraction)
                    .GetMethod(nameof(IExecutableInteraction.Execute))
                    .ReturnType,
                Is.EqualTo(typeof(InteractionResult)));
        }

        [Test]
        public void InteractionContext_WithValidValues_IsNormalizedAndValid()
        {
            var context = CreateContext(
                new RequestId(7U),
                new Vector3(2f, 0f, 0f));

            Assert.That(context.IsValid, Is.True);
            Assert.That(
                context.InteractionDirection,
                Is.EqualTo(Vector3.right));
            Assert.That(context.ExpectedTargetRevision, Is.EqualTo(0U));
        }

        [Test]
        public void InteractionContext_WithInvalidIdentityTimeOrDirection_IsInvalid()
        {
            var invalidPlayer = new InteractionContext(
                default,
                new RequestId(1U),
                1d,
                Vector3.zero,
                Vector3.forward);
            var invalidRequest = new InteractionContext(
                new PlayerId(1UL),
                default,
                1d,
                Vector3.zero,
                Vector3.forward);
            var invalidTime = new InteractionContext(
                new PlayerId(1UL),
                new RequestId(1U),
                double.NaN,
                Vector3.zero,
                Vector3.forward);
            var invalidDirection = new InteractionContext(
                new PlayerId(1UL),
                new RequestId(1U),
                1d,
                Vector3.zero,
                Vector3.zero);

            Assert.That(invalidPlayer.IsValid, Is.False);
            Assert.That(invalidRequest.IsValid, Is.False);
            Assert.That(invalidTime.IsValid, Is.False);
            Assert.That(invalidDirection.IsValid, Is.False);
        }

        [Test]
        public void PlayerAndRequestIds_UseStableNonZeroValueSemantics()
        {
            Assert.That(default(PlayerId).IsValid, Is.False);
            Assert.That(default(RequestId).IsValid, Is.False);
            Assert.That(new PlayerId(3UL), Is.EqualTo(new PlayerId(3UL)));
            Assert.That(new RequestId(5U), Is.EqualTo(new RequestId(5U)));
        }

        [Test]
        public void RequestIdGenerator_CreatesUniqueNonZeroIdsAndSkipsOverflowZero()
        {
            var generator = new InteractionRequestIdGenerator(uint.MaxValue);

            var first = generator.CreateNext();
            var second = generator.CreateNext();

            Assert.That(first.Value, Is.EqualTo(uint.MaxValue));
            Assert.That(second.Value, Is.EqualTo(1U));
            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void InteractionRequestAndResult_ExposeStructuredValidation()
        {
            var targetId =
                new InteractionTargetId("interaction.test.structured");
            var request = new InteractionRequest(
                CreateContext(new RequestId(1U), Vector3.forward),
                targetId);
            var success = InteractionResult.Create(
                request,
                InteractionResultCode.Success);
            var busy = InteractionResult.Create(
                request,
                InteractionResultCode.Busy);

            Assert.That(request.IsValid, Is.True);
            Assert.That(success.IsSuccess, Is.True);
            Assert.That(busy.IsSuccess, Is.False);
            Assert.That(
                Enum.GetValues(typeof(InteractionResultCode)),
                Is.SupersetOf(new object[]
                {
                    InteractionResultCode.Success,
                    InteractionResultCode.TargetInvalid,
                    InteractionResultCode.OutOfRange,
                    InteractionResultCode.Occluded,
                    InteractionResultCode.Busy,
                    InteractionResultCode.Cancelled,
                    InteractionResultCode.NotSupported,
                    InteractionResultCode.Unknown
                }));
        }

        [Test]
        public void LocalRequestPort_DeduplicatesWithoutApplyingTargetTwice()
        {
            var portObject = new GameObject("LocalRequestPortTest");
            var probeObject = new GameObject("ExecutionProbeTest");
            try
            {
                var port =
                    portObject.AddComponent<LocalInteractionRequestPort>();
                var probe =
                    probeObject.AddComponent<InteractionExecutionProbe>();
                Assert.That(
                    probe.Configure(
                        "interaction.test.deduplicate",
                        probeObject.transform,
                        true,
                        true,
                        0,
                        InteractionResultCode.Success),
                    Is.True);
                var request = new InteractionRequest(
                    CreateContext(
                        new RequestId(11U),
                        Vector3.forward),
                    probe.TargetId);

                var first = port.Execute(request, probe);
                var second = port.Execute(request, probe);

                Assert.That(first.Code, Is.EqualTo(InteractionResultCode.Success));
                Assert.That(second.Code, Is.EqualTo(first.Code));
                Assert.That(probe.ExecutionCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(portObject);
                UnityEngine.Object.DestroyImmediate(probeObject);
            }
        }

        [Test]
        public void ExecutionValidator_MapsRangeLayerOcclusionAndSupportFailures()
        {
            var settings =
                AssetDatabase.LoadAssetAtPath<InteractionSettings>(
                    InteractionSettingsPath);
            var originObject = new GameObject("ExecutionOrigin");
            var probeObject =
                GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject wall = null;

            try
            {
                Assert.That(settings, Is.Not.Null);
                probeObject.layer = LayerMask.NameToLayer("Interactable");
                probeObject.GetComponent<Collider>().isTrigger = true;
                probeObject.transform.position = Vector3.forward;
                var probe =
                    probeObject.AddComponent<InteractionExecutionProbe>();
                Assert.That(
                    probe.Configure(
                        "interaction.test.validation",
                        probeObject.transform,
                        true,
                        true,
                        0,
                        InteractionResultCode.Success),
                    Is.True);
                var request = new InteractionRequest(
                    CreateContext(
                        new RequestId(20U),
                        Vector3.forward),
                    probe.TargetId);
                var validator = new InteractionExecutionValidator();
                Physics.SyncTransforms();

                Assert.That(
                    validator.Validate(
                        request,
                        probe,
                        probe,
                        originObject.transform,
                        settings),
                    Is.EqualTo(InteractionResultCode.Success));

                probeObject.transform.position =
                    Vector3.forward *
                    (settings.DetectionRadius + 1f);
                Physics.SyncTransforms();
                Assert.That(
                    validator.Validate(
                        request,
                        probe,
                        probe,
                        originObject.transform,
                        settings),
                    Is.EqualTo(InteractionResultCode.OutOfRange));

                probeObject.transform.position = Vector3.forward;
                probeObject.layer = 0;
                Physics.SyncTransforms();
                Assert.That(
                    validator.Validate(
                        request,
                        probe,
                        probe,
                        originObject.transform,
                        settings),
                    Is.EqualTo(InteractionResultCode.LayerRejected));

                probeObject.layer =
                    LayerMask.NameToLayer("Interactable");
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = Vector3.forward * 0.5f;
                wall.transform.localScale =
                    new Vector3(1f, 1f, 0.25f);
                Physics.SyncTransforms();
                Assert.That(
                    validator.Validate(
                        request,
                        probe,
                        probe,
                        originObject.transform,
                        settings),
                    Is.EqualTo(InteractionResultCode.Occluded));

                Assert.That(
                    validator.Validate(
                        request,
                        probe,
                        null,
                        originObject.transform,
                        settings),
                    Is.EqualTo(InteractionResultCode.NotSupported));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(wall);
                UnityEngine.Object.DestroyImmediate(probeObject);
                UnityEngine.Object.DestroyImmediate(originObject);
            }
        }

        [Test]
        public void ApprovedInteractAction_IsButtonWithKeyboardAndGamepadBindings()
        {
            var inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    ProjectConstants.InputActionsAssetPath);

            Assert.That(
                InteractionInputConfigurationValidator.TryGetInteractAction(
                    inputActions,
                    out var interactAction,
                    out var error),
                Is.True,
                error);
            Assert.That(interactAction.type, Is.EqualTo(InputActionType.Button));
            var bindingPaths = interactAction.bindings
                .Select(binding => binding.path)
                .ToArray();
            Assert.That(bindingPaths, Does.Contain("<Keyboard>/e"));
            Assert.That(bindingPaths, Does.Contain("<Gamepad>/buttonWest"));
        }

        [Test]
        public void PlayerPrefab_HasValidExecutionComposition()
        {
            var playerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);

            Assert.That(playerPrefab, Is.Not.Null);
            Assert.That(
                playerPrefab.GetComponent<LocalPlayerIdentity>()
                    .HasValidIdentity,
                Is.True);
            Assert.That(
                playerPrefab.GetComponent<PlayerInteractionInputReader>()
                    .HasValidConfiguration,
                Is.True);
            Assert.That(
                playerPrefab.GetComponent<InteractionExecutor>()
                    .HasValidConfiguration,
                Is.True);
            Assert.That(
                playerPrefab.GetComponent<LocalInteractionRequestPort>(),
                Is.Not.Null);
        }

        [Test]
        public void ExecutorSource_DoesNotSearchSceneReadDevicesOrReferencePresentation()
        {
            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                ExecutorSourcePath);
            var source = File.ReadAllText(fullPath);

            Assert.That(source, Does.Not.Contain("FindObject"));
            Assert.That(source, Does.Not.Contain("OverlapSphere"));
            Assert.That(source, Does.Not.Contain("Keyboard.current"));
            Assert.That(source, Does.Not.Contain("Gamepad.current"));
            Assert.That(source, Does.Not.Contain("PlayerMovement"));
            Assert.That(source, Does.Not.Contain("PromptView"));
        }

        private static InteractionContext CreateContext(
            RequestId requestId,
            Vector3 direction)
        {
            return new InteractionContext(
                new PlayerId(1UL),
                requestId,
                1d,
                Vector3.zero,
                direction);
        }
    }
}
