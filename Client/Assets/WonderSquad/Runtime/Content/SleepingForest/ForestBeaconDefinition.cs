using System;
using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Content.SleepingForest
{
    /// <summary>
    /// Stores the immutable authored identity of one forest signal beacon.
    /// Runtime progress is owned by the scene trial instance.
    /// </summary>
    [Serializable]
    public sealed class ForestBeaconDefinition
    {
        [SerializeField]
        [Tooltip("Authored position occupied by this beacon in the trial order.")]
        private ForestBeaconSlot slot = ForestBeaconSlot.Unknown;

        [SerializeField]
        [Tooltip("Stable interaction identity expected from the configured scene beacon.")]
        private string targetId = string.Empty;

        public ForestBeaconSlot Slot => slot;

        public InteractionTargetId TargetId =>
            new InteractionTargetId(targetId);

        public bool IsValid =>
            slot != ForestBeaconSlot.Unknown &&
            TargetId.IsValid;

        /// <summary>
        /// Supplies immutable authoring data before the owning definition is
        /// consumed. Runtime gameplay must treat the definition as read-only.
        /// </summary>
        public bool Configure(
            ForestBeaconSlot beaconSlot,
            string stableTargetId)
        {
            slot = beaconSlot;
            targetId = stableTargetId ?? string.Empty;
            return IsValid;
        }
    }
}
