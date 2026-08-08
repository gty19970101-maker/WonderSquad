using UnityEngine;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Adapts one explicitly configured scene beacon to the existing
    /// interaction execution contract. Trial order remains owned by the
    /// ForestSignalTrial instance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ForestBeaconInteraction :
        MonoBehaviour,
        IExecutableInteraction
    {
        [SerializeField]
        [Tooltip("Component implementing the read-only detection target contract.")]
        private MonoBehaviour interactionTargetComponent;

        [SerializeField]
        [Tooltip("Scene-local trial that owns this beacon's runtime state.")]
        private ForestSignalTrial trial;

        [SerializeField]
        [Tooltip("Authored slot occupied by this beacon instance.")]
        private ForestBeaconSlot slot = ForestBeaconSlot.Unknown;

        private IInteractable interactionTarget;

        public ForestSignalTrial Trial => trial;

        public ForestBeaconSlot Slot => slot;

        public InteractionTargetId TargetId
        {
            get
            {
                ResolveTarget();
                return interactionTarget == null
                    ? default
                    : interactionTarget.TargetId;
            }
        }

        public bool IsExecutionAvailable
        {
            get
            {
                ResolveTarget();
                return
                    isActiveAndEnabled &&
                    HasValidConfiguration &&
                    interactionTarget.IsDetectionEnabled &&
                    trial.CanExecute(this);
            }
        }

        public bool HasValidConfiguration
        {
            get
            {
                ResolveTarget();
                return
                    interactionTargetComponent != null &&
                    interactionTarget != null &&
                    interactionTarget.TargetId.IsValid &&
                    trial != null &&
                    (slot == ForestBeaconSlot.BeaconA ||
                     slot == ForestBeaconSlot.BeaconB);
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetDetectionEnabled(false);
        }

        /// <summary>
        /// Supplies explicit composition references before gameplay starts.
        /// </summary>
        public bool Configure(
            MonoBehaviour targetComponent,
            ForestSignalTrial owningTrial,
            ForestBeaconSlot beaconSlot)
        {
            Unsubscribe();
            interactionTargetComponent = targetComponent;
            interactionTarget = targetComponent as IInteractable;
            trial = owningTrial;
            slot = beaconSlot;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            return HasValidConfiguration;
        }

        public InteractionResult Execute(in InteractionContext context)
        {
            var targetId = TargetId;
            if (!context.IsValid)
            {
                return new InteractionResult(
                    context.RequestId,
                    targetId,
                    InteractionResultCode.Unknown);
            }

            if (!HasValidConfiguration ||
                !trial.IsConfiguredSource(this))
            {
                return new InteractionResult(
                    context.RequestId,
                    targetId,
                    InteractionResultCode.TargetInvalid);
            }

            if (!IsExecutionAvailable)
            {
                return new InteractionResult(
                    context.RequestId,
                    targetId,
                    InteractionResultCode.Busy);
            }

            return new InteractionResult(
                context.RequestId,
                targetId,
                trial.TryActivate(this, context));
        }

        private void Subscribe()
        {
            ResolveTarget();
            if (trial == null || !HasValidConfiguration)
            {
                SetDetectionEnabled(false);
                return;
            }

            trial.TrialStateChanged -= OnTrialStateChanged;
            trial.TrialStateChanged += OnTrialStateChanged;
            ApplySnapshot(trial.CurrentSnapshot);
        }

        private void Unsubscribe()
        {
            if (trial != null)
            {
                trial.TrialStateChanged -= OnTrialStateChanged;
            }
        }

        private void OnTrialStateChanged(
            ForestSignalTrialSnapshot snapshot)
        {
            ApplySnapshot(snapshot);
        }

        private void ApplySnapshot(
            ForestSignalTrialSnapshot snapshot)
        {
            var beaconState = snapshot.GetBeacon(slot);
            var shouldDetect =
                trial != null &&
                trial.IsConfiguredSource(this) &&
                beaconState.Slot == slot &&
                !beaconState.IsActivated &&
                !snapshot.IsCompleted;
            SetDetectionEnabled(shouldDetect);
        }

        private void SetDetectionEnabled(bool shouldEnable)
        {
            if (interactionTargetComponent != null &&
                interactionTargetComponent != this &&
                interactionTargetComponent.enabled != shouldEnable)
            {
                interactionTargetComponent.enabled = shouldEnable;
            }
        }

        private void ResolveTarget()
        {
            if (interactionTarget == null &&
                interactionTargetComponent != null)
            {
                interactionTarget =
                    interactionTargetComponent as IInteractable;
            }
        }
    }
}
