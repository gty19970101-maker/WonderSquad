using UnityEngine;
using WonderSquad.Content.SleepingForest;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Maps one beacon's read-only trial state to isolated color and marker
    /// feedback without owning gameplay state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ForestBeaconVisual : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId =
            Shader.PropertyToID("_Color");

        [SerializeField]
        [Tooltip("Scene-local trial that supplies immutable snapshots.")]
        private ForestSignalTrial trial;

        [SerializeField]
        [Tooltip("Beacon slot represented by this visual instance.")]
        private ForestBeaconSlot slot = ForestBeaconSlot.Unknown;

        [SerializeField]
        [Tooltip("Renderer receiving an instance-only property block.")]
        private Renderer beaconRenderer;

        [SerializeField]
        [Tooltip("Shape marker visible while the beacon is dormant.")]
        private GameObject dormantMarker;

        [SerializeField]
        [Tooltip("Ring marker visible after activation.")]
        private GameObject activatedMarker;

        [SerializeField]
        [Tooltip("Error marker visible after an incorrect-order attempt.")]
        private GameObject incorrectMarker;

        [SerializeField]
        private Color dormantColor = new Color(0.25f, 0.55f, 0.65f, 1f);

        [SerializeField]
        private Color activatedColor = new Color(0.35f, 1f, 0.45f, 1f);

        [SerializeField]
        private Color incorrectColor = new Color(1f, 0.3f, 0.2f, 1f);

        private MaterialPropertyBlock materialPropertyBlock;
        private ForestBeaconVisualMode currentMode;
        private uint lastAppliedRevision;
        private bool hasAppliedSnapshot;

        public ForestSignalTrial Trial => trial;

        public ForestBeaconSlot Slot => slot;

        public ForestBeaconVisualMode CurrentMode => currentMode;

        public uint LastAppliedRevision => lastAppliedRevision;

        public bool HasValidConfiguration =>
            trial != null &&
            (slot == ForestBeaconSlot.BeaconA ||
             slot == ForestBeaconSlot.BeaconB) &&
            beaconRenderer != null &&
            dormantMarker != null &&
            activatedMarker != null &&
            incorrectMarker != null &&
            dormantMarker != gameObject &&
            activatedMarker != gameObject &&
            incorrectMarker != gameObject;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Supplies explicit scene references and presentation values before
        /// gameplay starts.
        /// </summary>
        public bool Configure(
            ForestSignalTrial owningTrial,
            ForestBeaconSlot beaconSlot,
            Renderer renderer,
            GameObject dormantStateMarker,
            GameObject activatedStateMarker,
            GameObject incorrectStateMarker,
            Color dormantStateColor,
            Color activatedStateColor,
            Color incorrectStateColor)
        {
            Unsubscribe();
            trial = owningTrial;
            slot = beaconSlot;
            beaconRenderer = renderer;
            dormantMarker = dormantStateMarker;
            activatedMarker = activatedStateMarker;
            incorrectMarker = incorrectStateMarker;
            dormantColor = dormantStateColor;
            activatedColor = activatedStateColor;
            incorrectColor = incorrectStateColor;
            hasAppliedSnapshot = false;
            currentMode = ForestBeaconVisualMode.Unknown;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            return HasValidConfiguration;
        }

        private void Subscribe()
        {
            if (!HasValidConfiguration)
            {
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
            if (!HasValidConfiguration ||
                (hasAppliedSnapshot &&
                 lastAppliedRevision == snapshot.Revision))
            {
                return;
            }

            var beaconState = snapshot.GetBeacon(slot);
            var visualMode = ResolveVisualMode(snapshot, beaconState);
            ApplyMode(visualMode);
            lastAppliedRevision = snapshot.Revision;
            hasAppliedSnapshot = true;
        }

        private ForestBeaconVisualMode ResolveVisualMode(
            ForestSignalTrialSnapshot snapshot,
            ForestBeaconRuntimeState beaconState)
        {
            if (beaconState.IsActivated)
            {
                return ForestBeaconVisualMode.Activated;
            }

            if (snapshot.HasIncorrectOrder &&
                snapshot.LastAttemptedSlot == slot)
            {
                return ForestBeaconVisualMode.Incorrect;
            }

            return ForestBeaconVisualMode.Dormant;
        }

        private void ApplyMode(ForestBeaconVisualMode visualMode)
        {
            currentMode = visualMode;
            SetMarkerActive(
                dormantMarker,
                visualMode == ForestBeaconVisualMode.Dormant);
            SetMarkerActive(
                activatedMarker,
                visualMode == ForestBeaconVisualMode.Activated);
            SetMarkerActive(
                incorrectMarker,
                visualMode == ForestBeaconVisualMode.Incorrect);

            materialPropertyBlock ??= new MaterialPropertyBlock();
            var displayColor = ResolveColor(visualMode);
            beaconRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetColor(
                BaseColorPropertyId,
                displayColor);
            materialPropertyBlock.SetColor(
                ColorPropertyId,
                displayColor);
            beaconRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        private Color ResolveColor(ForestBeaconVisualMode visualMode)
        {
            switch (visualMode)
            {
                case ForestBeaconVisualMode.Activated:
                    return activatedColor;

                case ForestBeaconVisualMode.Incorrect:
                    return incorrectColor;

                default:
                    return dormantColor;
            }
        }

        private static void SetMarkerActive(
            GameObject marker,
            bool shouldBeActive)
        {
            if (marker != null && marker.activeSelf != shouldBeActive)
            {
                marker.SetActive(shouldBeActive);
            }
        }
    }
}
