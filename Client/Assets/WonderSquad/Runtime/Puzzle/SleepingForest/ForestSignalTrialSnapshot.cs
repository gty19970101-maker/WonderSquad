using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Exposes the complete read-only state required by presentation and the
    /// future Sprint003C environment consequence adapter.
    /// </summary>
    public readonly struct ForestSignalTrialSnapshot
    {
        public ForestSignalTrialSnapshot(
            ForestSignalTrialPhase phase,
            ForestBeaconRuntimeState beaconA,
            ForestBeaconRuntimeState beaconB,
            InteractionTargetId lastAttemptedTargetId,
            ForestBeaconSlot lastAttemptedSlot,
            ForestSignalTrialOutcome lastOutcome,
            uint revision)
        {
            Phase = phase;
            BeaconA = beaconA;
            BeaconB = beaconB;
            LastAttemptedTargetId = lastAttemptedTargetId;
            LastAttemptedSlot = lastAttemptedSlot;
            LastOutcome = lastOutcome;
            Revision = revision;
        }

        public ForestSignalTrialPhase Phase { get; }

        public ForestBeaconRuntimeState BeaconA { get; }

        public ForestBeaconRuntimeState BeaconB { get; }

        public InteractionTargetId LastAttemptedTargetId { get; }

        public ForestBeaconSlot LastAttemptedSlot { get; }

        public ForestSignalTrialOutcome LastOutcome { get; }

        public uint Revision { get; }

        public bool HasIncorrectOrder =>
            LastOutcome == ForestSignalTrialOutcome.IncorrectOrder;

        public bool IsCompleted =>
            Phase == ForestSignalTrialPhase.Completed;

        public ForestBeaconRuntimeState GetBeacon(
            ForestBeaconSlot slot)
        {
            return slot == ForestBeaconSlot.BeaconA
                ? BeaconA
                : slot == ForestBeaconSlot.BeaconB
                    ? BeaconB
                    : default;
        }
    }
}
