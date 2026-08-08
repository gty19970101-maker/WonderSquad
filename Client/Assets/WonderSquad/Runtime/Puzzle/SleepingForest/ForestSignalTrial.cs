using System;
using UnityEngine;
using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Logging;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Owns the local scene-instance truth for the fixed forest beacon trial.
    /// It exposes state but never applies environment consequences.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ForestSignalTrial : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Immutable content definition for this scene trial instance.")]
        private ForestSignalTrialDefinition trialDefinition;

        [SerializeField]
        [Tooltip("Scene instance registered as Beacon A.")]
        private ForestBeaconInteraction beaconA;

        [SerializeField]
        [Tooltip("Scene instance registered as Beacon B.")]
        private ForestBeaconInteraction beaconB;

        private ForestSignalTrialSnapshot currentSnapshot;
        private bool hasInitialized;
        private bool hasReportedInvalidConfiguration;

        /// <summary>
        /// Raised once after an accepted attempt commits a new revision.
        /// This event belongs only to this scene trial instance.
        /// </summary>
        public event Action<ForestSignalTrialSnapshot> TrialStateChanged;

        public ForestSignalTrialDefinition TrialDefinition =>
            trialDefinition;

        public ForestBeaconInteraction BeaconA => beaconA;

        public ForestBeaconInteraction BeaconB => beaconB;

        public ForestSignalTrialSnapshot CurrentSnapshot
        {
            get
            {
                EnsureInitialized();
                return currentSnapshot;
            }
        }

        public bool HasValidConfiguration =>
            trialDefinition != null &&
            trialDefinition.IsValid &&
            beaconA != null &&
            beaconB != null &&
            beaconA != beaconB &&
            beaconA.HasValidConfiguration &&
            beaconB.HasValidConfiguration &&
            beaconA.Trial == this &&
            beaconB.Trial == this &&
            beaconA.Slot == ForestBeaconSlot.BeaconA &&
            beaconB.Slot == ForestBeaconSlot.BeaconB &&
            beaconA.TargetId == trialDefinition.BeaconA.TargetId &&
            beaconB.TargetId == trialDefinition.BeaconB.TargetId &&
            IsInSameScene(beaconA) &&
            IsInSameScene(beaconB);

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Supplies explicit scene references before the trial starts. This
        /// is intended for authoring and isolated tests, not live reconfiguration.
        /// </summary>
        public bool Configure(
            ForestSignalTrialDefinition definition,
            ForestBeaconInteraction firstBeacon,
            ForestBeaconInteraction secondBeacon)
        {
            if (hasInitialized && currentSnapshot.Revision != 0U)
            {
                ProjectLog.Error(
                    "ForestSignalTrial cannot be reconfigured after progress has started.");
                return false;
            }

            trialDefinition = definition;
            beaconA = firstBeacon;
            beaconB = secondBeacon;
            hasInitialized = false;
            hasReportedInvalidConfiguration = false;
            currentSnapshot = default;
            return EnsureInitialized();
        }

        internal bool IsConfiguredSource(
            ForestBeaconInteraction source)
        {
            if (!EnsureInitialized() || source == null)
            {
                return false;
            }

            if (source != beaconA && source != beaconB)
            {
                return false;
            }

            if (!IsInSameScene(source))
            {
                return false;
            }

            var expectedState = currentSnapshot.GetBeacon(source.Slot);
            return
                expectedState.Slot == source.Slot &&
                expectedState.TargetId == source.TargetId;
        }

        internal bool CanExecute(
            ForestBeaconInteraction source)
        {
            if (!isActiveAndEnabled ||
                !IsConfiguredSource(source) ||
                currentSnapshot.IsCompleted)
            {
                return false;
            }

            return !currentSnapshot.GetBeacon(source.Slot).IsActivated;
        }

        internal InteractionResultCode TryActivate(
            ForestBeaconInteraction source,
            in InteractionContext context)
        {
            if (!context.IsValid)
            {
                return InteractionResultCode.Unknown;
            }

            if (!IsConfiguredSource(source))
            {
                return InteractionResultCode.TargetInvalid;
            }

            if (!CanExecute(source))
            {
                return InteractionResultCode.Busy;
            }

            if (currentSnapshot.Revision == uint.MaxValue)
            {
                ProjectLog.Error(
                    "ForestSignalTrial revision limit was reached; state was not changed.");
                return InteractionResultCode.Busy;
            }

            switch (source.Slot)
            {
                case ForestBeaconSlot.BeaconA:
                    return TryActivateBeaconA(source.TargetId);

                case ForestBeaconSlot.BeaconB:
                    return TryActivateBeaconB(source.TargetId);

                default:
                    return InteractionResultCode.TargetInvalid;
            }
        }

        private InteractionResultCode TryActivateBeaconA(
            InteractionTargetId targetId)
        {
            if (currentSnapshot.Phase !=
                ForestSignalTrialPhase.AwaitingFirstBeacon)
            {
                return InteractionResultCode.Busy;
            }

            Commit(
                ForestSignalTrialPhase.AwaitingSecondBeacon,
                ForestBeaconLogicalState.Activated,
                ForestBeaconLogicalState.Dormant,
                targetId,
                ForestBeaconSlot.BeaconA,
                ForestSignalTrialOutcome.FirstBeaconActivated);
            return InteractionResultCode.Success;
        }

        private InteractionResultCode TryActivateBeaconB(
            InteractionTargetId targetId)
        {
            if (currentSnapshot.Phase ==
                ForestSignalTrialPhase.AwaitingFirstBeacon)
            {
                Commit(
                    ForestSignalTrialPhase.AwaitingFirstBeacon,
                    ForestBeaconLogicalState.Dormant,
                    ForestBeaconLogicalState.Dormant,
                    targetId,
                    ForestBeaconSlot.BeaconB,
                    ForestSignalTrialOutcome.IncorrectOrder);
                return InteractionResultCode.Success;
            }

            if (currentSnapshot.Phase ==
                ForestSignalTrialPhase.AwaitingSecondBeacon)
            {
                Commit(
                    ForestSignalTrialPhase.Completed,
                    ForestBeaconLogicalState.Activated,
                    ForestBeaconLogicalState.Activated,
                    targetId,
                    ForestBeaconSlot.BeaconB,
                    ForestSignalTrialOutcome.TrialCompleted);
                return InteractionResultCode.Success;
            }

            return InteractionResultCode.Busy;
        }

        private void Commit(
            ForestSignalTrialPhase phase,
            ForestBeaconLogicalState beaconAState,
            ForestBeaconLogicalState beaconBState,
            InteractionTargetId attemptedTargetId,
            ForestBeaconSlot attemptedSlot,
            ForestSignalTrialOutcome outcome)
        {
            var nextRevision = currentSnapshot.Revision + 1U;
            currentSnapshot = new ForestSignalTrialSnapshot(
                phase,
                new ForestBeaconRuntimeState(
                    ForestBeaconSlot.BeaconA,
                    currentSnapshot.BeaconA.TargetId,
                    beaconAState),
                new ForestBeaconRuntimeState(
                    ForestBeaconSlot.BeaconB,
                    currentSnapshot.BeaconB.TargetId,
                    beaconBState),
                attemptedTargetId,
                attemptedSlot,
                outcome,
                nextRevision);
            TrialStateChanged?.Invoke(currentSnapshot);
        }

        private bool EnsureInitialized()
        {
            if (hasInitialized)
            {
                return true;
            }

            if (!HasValidConfiguration)
            {
                ReportInvalidConfiguration();
                return false;
            }

            currentSnapshot = new ForestSignalTrialSnapshot(
                ForestSignalTrialPhase.AwaitingFirstBeacon,
                new ForestBeaconRuntimeState(
                    ForestBeaconSlot.BeaconA,
                    trialDefinition.BeaconA.TargetId,
                    ForestBeaconLogicalState.Dormant),
                new ForestBeaconRuntimeState(
                    ForestBeaconSlot.BeaconB,
                    trialDefinition.BeaconB.TargetId,
                    ForestBeaconLogicalState.Dormant),
                default,
                ForestBeaconSlot.Unknown,
                ForestSignalTrialOutcome.None,
                0U);
            hasInitialized = true;
            return true;
        }

        private bool IsInSameScene(Component component)
        {
            if (component == null)
            {
                return false;
            }

            var trialScene = gameObject.scene;
            var componentScene = component.gameObject.scene;
            return
                trialScene.IsValid() &&
                componentScene.IsValid() &&
                componentScene == trialScene;
        }

        private void ReportInvalidConfiguration()
        {
            if (hasReportedInvalidConfiguration)
            {
                return;
            }

            hasReportedInvalidConfiguration = true;
            ProjectLog.Error(
                "ForestSignalTrial requires one valid definition and two distinct, explicitly configured scene beacons.");
        }
    }
}
