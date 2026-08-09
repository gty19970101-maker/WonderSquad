using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Interaction.Detection;
using WonderSquad.Player.Spawning;
using WonderSquad.Puzzle.SleepingForest;
using WonderSquad.SleepingForest.Completion;
using WonderSquad.UI.SleepingForest;

namespace WonderSquad.Tests.EditMode
{
    public sealed class SleepingForestCompletionEditModeTests
    {
        private const string SleepingForestScenePath =
            "Assets/WonderSquad/Scenes/Gameplay/SleepingForest/SleepingForest.unity";
        private const string SleepingForestAssemblyPath =
            "Assets/WonderSquad/Runtime/Level/SleepingForest/WonderSquad.SleepingForest.asmdef";

        [Test]
        public void InitialState_IsNotCompletedAndFeedbackIsHidden()
        {
            using (var fixture = new CompletionFixture())
            {
                Assert.That(
                    fixture.Controller.CurrentSnapshot.State,
                    Is.EqualTo(SleepingForestCompletionState.NotCompleted));
                Assert.That(
                    fixture.Controller.CurrentSnapshot.CompletionRevision,
                    Is.Zero);
                Assert.That(
                    fixture.Controller.FeedbackState,
                    Is.EqualTo(
                        SleepingForestCompletionFeedbackState.Hidden));
            }
        }

        [Test]
        public void PlayerAtEndWithoutCompletedTrial_DoesNotComplete()
        {
            using (var fixture = new CompletionFixture())
            {
                fixture.EnterEnd();

                Assert.That(
                    fixture.Controller.CurrentSnapshot.IsCompleted,
                    Is.False);
                Assert.That(
                    fixture.Controller.FeedbackState,
                    Is.EqualTo(
                        SleepingForestCompletionFeedbackState.ConditionsUnmet));
            }
        }

        [Test]
        public void CompletedTrialWithoutPlayerAtEnd_DoesNotComplete()
        {
            using (var fixture = new CompletionFixture())
            {
                fixture.CompleteTrial();

                Assert.That(
                    fixture.Controller.CurrentSnapshot.IsCompleted,
                    Is.False);
                Assert.That(
                    fixture.Controller.FeedbackState,
                    Is.EqualTo(
                        SleepingForestCompletionFeedbackState.Hidden));
            }
        }

        [Test]
        public void PlayerAtEndThenCompletedTrial_CompletesImmediately()
        {
            using (var fixture = new CompletionFixture())
            {
                fixture.EnterEnd();
                fixture.CompleteTrial();

                AssertCompleted(fixture);
            }
        }

        [Test]
        public void CompletedTrialThenPlayerAtEnd_Completes()
        {
            using (var fixture = new CompletionFixture())
            {
                fixture.CompleteTrial();
                fixture.EnterEnd();

                AssertCompleted(fixture);
            }
        }

        [Test]
        public void CompletionAndDuplicatePresence_AreIdempotent()
        {
            using (var fixture = new CompletionFixture())
            {
                var completionEventCount = 0;
                fixture.Controller.Completed += snapshot =>
                    completionEventCount++;
                fixture.CompleteTrial();
                fixture.EnterEnd();
                fixture.EnterEnd();
                fixture.ExitEnd();
                fixture.EnterEnd();
                fixture.Controller.enabled = false;
                fixture.Controller.enabled = true;

                Assert.That(completionEventCount, Is.EqualTo(1));
                Assert.That(
                    fixture.Controller.CurrentSnapshot.CompletionRevision,
                    Is.EqualTo(1U));
            }
        }

        [Test]
        public void IncorrectOrder_DoesNotCompleteAndCanRecover()
        {
            using (var fixture = new CompletionFixture())
            {
                fixture.EnterEnd();
                fixture.Execute(fixture.BeaconB, 1U);
                Assert.That(
                    fixture.Trial.CurrentSnapshot.HasIncorrectOrder,
                    Is.True);
                Assert.That(
                    fixture.Controller.CurrentSnapshot.IsCompleted,
                    Is.False);

                fixture.Execute(fixture.BeaconA, 2U);
                fixture.Execute(fixture.BeaconB, 3U);

                AssertCompleted(fixture);
            }
        }

        [Test]
        public void Trigger_RecognizesOnlySpawnedPlayerCollider()
        {
            using (var fixture = new CompletionFixture())
            {
                var environmentObject =
                    new GameObject("EnvironmentCollider");
                var environmentCollider =
                    environmentObject.AddComponent<BoxCollider>();
                try
                {
                    Assert.That(
                        fixture.Trigger.IsOfficialPlayerCollider(
                            environmentCollider),
                        Is.False);
                    Assert.That(
                        fixture.Trigger.IsOfficialPlayerCollider(
                            fixture.PlayerCollider),
                        Is.True);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(environmentObject);
                }
            }
        }

        [Test]
        public void Views_RenderOnlyChangedFeedbackAndNeverBlockRaycasts()
        {
            using (var fixture = new CompletionFixture())
            {
                Assert.That(fixture.CompletionView.IsVisible, Is.False);
                Assert.That(
                    fixture.CompletedMarker.activeSelf,
                    Is.False);

                fixture.EnterEnd();
                Assert.That(fixture.CompletionView.IsVisible, Is.True);
                Assert.That(
                    fixture.CompletionView.DisplayedMessage,
                    Is.EqualTo(
                        SleepingForestCompletionPresenter
                            .RequirementFallbackMessage));
                var unmetRefreshCount =
                    fixture.CompletionView.RefreshCount;
                fixture.EnterEnd();
                Assert.That(
                    fixture.CompletionView.RefreshCount,
                    Is.EqualTo(unmetRefreshCount));

                fixture.CompleteTrial();
                Assert.That(
                    fixture.CompletionView.DisplayedMessage,
                    Is.EqualTo(
                        SleepingForestCompletionPresenter
                            .CompletionFallbackMessage));
                Assert.That(fixture.CompletedMarker.activeSelf, Is.True);
                Assert.That(fixture.MessageText.raycastTarget, Is.False);
            }
        }

        [Test]
        public void Snapshot_IsReadonlyValueWithoutUnityObjectReferences()
        {
            var snapshotType = typeof(SleepingForestCompletionSnapshot);

            Assert.That(snapshotType.IsValueType, Is.True);
            Assert.That(
                snapshotType.GetFields()
                    .Any(field =>
                        typeof(UnityEngine.Object).IsAssignableFrom(
                            field.FieldType)),
                Is.False);
        }

        [Test]
        public void SleepingForestAssembly_HasOnlyApprovedRuntimeReferences()
        {
            var assemblyDefinition =
                File.ReadAllText(SleepingForestAssemblyPath);

            Assert.That(
                assemblyDefinition,
                Does.Contain("WonderSquad.Player"));
            Assert.That(
                assemblyDefinition,
                Does.Contain("WonderSquad.Puzzle"));
            Assert.That(
                assemblyDefinition,
                Does.Not.Contain("WonderSquad.UI"));
            Assert.That(
                assemblyDefinition,
                Does.Not.Contain("WonderSquad.Interaction"));
            Assert.That(
                assemblyDefinition,
                Does.Not.Contain("WonderSquad.Network"));
        }

        [Test]
        public void SleepingForestScene_HasOneValidCompletionComposition()
        {
            EditorSceneManager.OpenScene(
                SleepingForestScenePath,
                OpenSceneMode.Single);
            try
            {
                var controllers =
                    UnityEngine.Object.FindObjectsByType<
                        SleepingForestCompletionController>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);
                var triggers =
                    UnityEngine.Object.FindObjectsByType<
                        SleepingForestSliceEndTrigger>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);
                var presenters =
                    UnityEngine.Object.FindObjectsByType<
                        SleepingForestCompletionPresenter>(
                            FindObjectsInactive.Include,
                            FindObjectsSortMode.None);

                Assert.That(controllers, Has.Length.EqualTo(1));
                Assert.That(triggers, Has.Length.EqualTo(1));
                Assert.That(presenters, Has.Length.EqualTo(1));
                Assert.That(controllers[0].HasValidConfiguration, Is.True);
                Assert.That(triggers[0].HasValidConfiguration, Is.True);
                Assert.That(triggers[0].TriggerCollider.isTrigger, Is.True);
                Assert.That(presenters[0].HasValidConfiguration, Is.True);
                Assert.That(
                    triggers[0].TriggerCollider.bounds.Intersects(
                        FindSceneObject("SliceEndGround")
                            .GetComponent<Renderer>().bounds),
                    Is.True);
                Assert.That(
                    FindSceneObject("SliceEndLandmark_Tower"),
                    Is.Not.Null);
                Assert.That(
                    FindSceneObject("SliceEndLandmark_Arch"),
                    Is.Not.Null);
            }
            finally
            {
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);
            }
        }

        [Test]
        public void CompletionAuthoring_IsManualAndHasNoAutomaticEntryPoint()
        {
            var authoringType = typeof(WonderSquad.Editor
                .SleepingForestCompletionAuthoring);

            Assert.That(
                authoringType.GetCustomAttributes(
                    typeof(InitializeOnLoadAttribute),
                    false),
                Is.Empty);
            Assert.That(
                authoringType.GetMethods()
                    .Any(method => method.Name == "ApplyFromMenu"),
                Is.True);
        }

        private static void AssertCompleted(CompletionFixture fixture)
        {
            Assert.That(
                fixture.Controller.CurrentSnapshot.State,
                Is.EqualTo(SleepingForestCompletionState.Completed));
            Assert.That(
                fixture.Controller.CurrentSnapshot.CompletionRevision,
                Is.EqualTo(1U));
            Assert.That(
                fixture.Controller.CurrentSnapshot.TrialRevisionAtCompletion,
                Is.EqualTo(fixture.Trial.CurrentSnapshot.Revision));
            Assert.That(
                fixture.Controller.FeedbackState,
                Is.EqualTo(
                    SleepingForestCompletionFeedbackState.Completed));
        }

        private static GameObject FindSceneObject(string name)
        {
            return SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name)
                ?.gameObject;
        }

        private sealed class CompletionFixture : IDisposable
        {
            private const string BeaconATargetId =
                "sleeping_forest.beacon.a";
            private const string BeaconBTargetId =
                "sleeping_forest.beacon.b";

            private readonly GameObject root;
            private readonly GameObject playerTemplate;

            public CompletionFixture()
            {
                root = new GameObject("SleepingForestCompletionFixture");
                root.SetActive(false);

                Definition = ScriptableObject.CreateInstance<
                    ForestSignalTrialDefinition>();
                Definition.Configure(
                    "sleeping_forest.forest_signal_trial",
                    1,
                    BeaconATargetId,
                    BeaconBTargetId);
                Trial = CreateChild("ForestSignalTrial")
                    .AddComponent<ForestSignalTrial>();
                BeaconA = CreateBeacon(
                    "BeaconA",
                    BeaconATargetId,
                    ForestBeaconSlot.BeaconA);
                BeaconB = CreateBeacon(
                    "BeaconB",
                    BeaconBTargetId,
                    ForestBeaconSlot.BeaconB);
                Assert.That(
                    Trial.Configure(Definition, BeaconA, BeaconB),
                    Is.True);

                var spawnPoint = CreateChild("SpawnPoint")
                    .AddComponent<PlayerSpawnPoint>();
                Spawner = CreateChild("PlayerSpawner")
                    .AddComponent<PlayerSpawner>();
                playerTemplate = new GameObject("PlayerTemplate");
                playerTemplate.AddComponent<CharacterController>();
                Spawner.Configure(playerTemplate, spawnPoint);
                Assert.That(Spawner.TrySpawn(out var player), Is.True);
                Player = player;
                PlayerCollider = Player.GetComponent<CharacterController>();

                var triggerObject = CreateChild("SliceEndTrigger");
                triggerObject.SetActive(false);
                var boxCollider = triggerObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                Trigger = triggerObject
                    .AddComponent<SleepingForestSliceEndTrigger>();
                Assert.That(
                    Trigger.Configure(Spawner, boxCollider),
                    Is.True);
                triggerObject.SetActive(true);

                var controllerObject = CreateChild("CompletionController");
                controllerObject.SetActive(false);
                Controller = controllerObject
                    .AddComponent<SleepingForestCompletionController>();
                Assert.That(
                    Controller.Configure(Trial, Trigger),
                    Is.True);

                var viewRoot = CreateChild("CompletionView");
                viewRoot.SetActive(false);
                var contentRoot = CreateChild("ContentRoot");
                contentRoot.transform.SetParent(viewRoot.transform, false);
                var textObject = CreateChild("MessageText");
                textObject.transform.SetParent(contentRoot.transform, false);
                MessageText = textObject.AddComponent<Text>();
                MessageText.raycastTarget = false;
                CompletionView = viewRoot
                    .AddComponent<SleepingForestCompletionView>();
                Assert.That(
                    CompletionView.Configure(contentRoot, MessageText),
                    Is.True);

                var markerViewObject = CreateChild("MarkerView");
                markerViewObject.SetActive(false);
                CompletedMarker = CreateChild("CompletedMarker");
                CompletedMarker.transform.SetParent(
                    markerViewObject.transform,
                    false);
                MarkerView = markerViewObject
                    .AddComponent<SleepingForestCompletionMarkerView>();
                Assert.That(
                    MarkerView.Configure(CompletedMarker),
                    Is.True);

                Presenter = viewRoot
                    .AddComponent<SleepingForestCompletionPresenter>();
                Assert.That(
                    Presenter.Configure(
                        Controller,
                        CompletionView,
                        MarkerView),
                    Is.True);

                controllerObject.SetActive(true);
                markerViewObject.SetActive(true);
                viewRoot.SetActive(true);
                root.SetActive(true);
                InvokeLifecycle(Trigger, "OnEnable");
                InvokeLifecycle(Controller, "OnEnable");
                InvokeLifecycle(CompletionView, "OnEnable");
                InvokeLifecycle(MarkerView, "OnEnable");
                InvokeLifecycle(Presenter, "OnEnable");
            }

            public ForestSignalTrialDefinition Definition { get; }

            public ForestSignalTrial Trial { get; }

            public ForestBeaconInteraction BeaconA { get; }

            public ForestBeaconInteraction BeaconB { get; }

            public PlayerSpawner Spawner { get; }

            public GameObject Player { get; }

            public CharacterController PlayerCollider { get; }

            public SleepingForestSliceEndTrigger Trigger { get; }

            public SleepingForestCompletionController Controller { get; }

            public SleepingForestCompletionPresenter Presenter { get; }

            public SleepingForestCompletionView CompletionView { get; }

            public SleepingForestCompletionMarkerView MarkerView { get; }

            public GameObject CompletedMarker { get; }

            public Text MessageText { get; }

            public void EnterEnd()
            {
                InvokeTriggerMessage("OnTriggerEnter");
            }

            public void ExitEnd()
            {
                InvokeTriggerMessage("OnTriggerExit");
            }

            public void CompleteTrial()
            {
                Execute(BeaconA, 1U);
                Execute(BeaconB, 2U);
            }

            public void Execute(
                ForestBeaconInteraction beacon,
                uint requestId)
            {
                var result = beacon.Execute(new InteractionContext(
                    new PlayerId(1UL),
                    new RequestId(requestId),
                    requestId,
                    Vector3.zero,
                    Vector3.forward));
                Assert.That(
                    result.Code,
                    Is.EqualTo(InteractionResultCode.Success));
            }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(Player);
                UnityEngine.Object.DestroyImmediate(playerTemplate);
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(Definition);
            }

            private ForestBeaconInteraction CreateBeacon(
                string name,
                string targetId,
                ForestBeaconSlot slot)
            {
                var beaconObject = CreateChild(name);
                var target = beaconObject.AddComponent<InteractionTarget>();
                target.Configure(
                    targetId,
                    beaconObject.transform,
                    true,
                    0);
                var interaction = beaconObject
                    .AddComponent<ForestBeaconInteraction>();
                interaction.Configure(target, Trial, slot);
                return interaction;
            }

            private GameObject CreateChild(string name)
            {
                var child = new GameObject(name);
                child.transform.SetParent(root.transform, false);
                return child;
            }

            private void InvokeTriggerMessage(string methodName)
            {
                var method = typeof(SleepingForestSliceEndTrigger)
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(Trigger, new object[] { PlayerCollider });
            }

            private static void InvokeLifecycle(
                MonoBehaviour component,
                string methodName)
            {
                var method = component.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(component, null);
            }
        }
    }
}
