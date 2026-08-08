using System;
using System.Collections.Generic;
using UnityEngine;
using WonderSquad.Content.Definitions;
using WonderSquad.Content.Validation;

namespace WonderSquad.Content.SleepingForest
{
    /// <summary>
    /// Defines the static identity and fixed A-to-B order of the forest
    /// signal trial. It never stores scene progress.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ForestSignalTrialDefinition",
        menuName = "Wonder Squad/Sleeping Forest/Forest Signal Trial Definition")]
    public sealed class ForestSignalTrialDefinition :
        ScriptableObject,
        IContentDescriptor
    {
        private static readonly string[] EmptyReferencedIds =
            Array.Empty<string>();

        [SerializeField]
        [Tooltip("Stable content identity for this trial definition.")]
        private string stableId = string.Empty;

        [SerializeField]
        [Tooltip("Version of this definition schema.")]
        private int schemaVersion = 1;

        [SerializeField]
        [Tooltip("First beacon in the fixed trial order.")]
        private ForestBeaconDefinition beaconA = new ForestBeaconDefinition();

        [SerializeField]
        [Tooltip("Second beacon in the fixed trial order.")]
        private ForestBeaconDefinition beaconB = new ForestBeaconDefinition();

        public string StableId => stableId;

        public int SchemaVersion => schemaVersion;

        public IReadOnlyList<string> ReferencedIds => EmptyReferencedIds;

        public ForestBeaconDefinition BeaconA => beaconA;

        public ForestBeaconDefinition BeaconB => beaconB;

        public bool IsValid =>
            ContentValidator.IsStableIdFormatValid(stableId) &&
            schemaVersion > 0 &&
            beaconA != null &&
            beaconB != null &&
            beaconA.IsValid &&
            beaconB.IsValid &&
            beaconA.Slot == ForestBeaconSlot.BeaconA &&
            beaconB.Slot == ForestBeaconSlot.BeaconB &&
            beaconA.TargetId != beaconB.TargetId;

        /// <summary>
        /// Supplies authoring data before the asset is used. Gameplay code
        /// must never call this method on a live definition.
        /// </summary>
        public bool Configure(
            string trialStableId,
            int definitionSchemaVersion,
            string beaconATargetId,
            string beaconBTargetId)
        {
            stableId = trialStableId ?? string.Empty;
            schemaVersion = definitionSchemaVersion;
            beaconA ??= new ForestBeaconDefinition();
            beaconB ??= new ForestBeaconDefinition();
            beaconA.Configure(
                ForestBeaconSlot.BeaconA,
                beaconATargetId);
            beaconB.Configure(
                ForestBeaconSlot.BeaconB,
                beaconBTargetId);
            return IsValid;
        }
    }
}
