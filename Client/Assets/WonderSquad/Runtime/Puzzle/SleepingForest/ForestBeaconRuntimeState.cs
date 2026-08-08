using WonderSquad.Content.SleepingForest;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Provides an immutable snapshot of one configured beacon.
    /// </summary>
    public readonly struct ForestBeaconRuntimeState
    {
        public ForestBeaconRuntimeState(
            ForestBeaconSlot slot,
            InteractionTargetId targetId,
            ForestBeaconLogicalState logicalState)
        {
            Slot = slot;
            TargetId = targetId;
            LogicalState = logicalState;
        }

        public ForestBeaconSlot Slot { get; }

        public InteractionTargetId TargetId { get; }

        public ForestBeaconLogicalState LogicalState { get; }

        public bool IsActivated =>
            LogicalState == ForestBeaconLogicalState.Activated;
    }
}
