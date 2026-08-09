using UnityEngine;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Applies instance-local visual feedback for the advantage span. It uses
    /// property blocks so neither material assets nor other span instances are
    /// affected.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RootBridgeAdvantageVisual : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId =
            Shader.PropertyToID("_Color");

        [SerializeField]
        [Tooltip("Renderer receiving an instance-only shortcut color.")]
        private Renderer spanRenderer;

        [SerializeField]
        [Tooltip("Non-blocking marker visible before the trial is complete.")]
        private GameObject dormantMarker;

        [SerializeField]
        [Tooltip("Non-color route marker visible after the trial is complete.")]
        private GameObject activatedMarker;

        [SerializeField]
        private Color dormantColor = new Color(0.34f, 0.20f, 0.10f, 1f);

        [SerializeField]
        private Color activatedColor = new Color(0.35f, 1f, 0.45f, 1f);

        private MaterialPropertyBlock materialPropertyBlock;
        private SleepingForestRootBridgeState currentState;
        private bool hasAppliedState;

        public SleepingForestRootBridgeState CurrentState => currentState;

        public bool HasValidConfiguration =>
            spanRenderer != null &&
            dormantMarker != null &&
            activatedMarker != null &&
            dormantMarker != gameObject &&
            activatedMarker != gameObject;

        /// <summary>
        /// Supplies explicit local presentation references before use.
        /// </summary>
        public bool Configure(
            Renderer shortcutRenderer,
            GameObject dormantStateMarker,
            GameObject activatedStateMarker,
            Color shortcutDormantColor,
            Color shortcutActivatedColor)
        {
            spanRenderer = shortcutRenderer;
            dormantMarker = dormantStateMarker;
            activatedMarker = activatedStateMarker;
            dormantColor = shortcutDormantColor;
            activatedColor = shortcutActivatedColor;
            return HasValidConfiguration;
        }

        /// <summary>
        /// Applies only presentation derived from a local bridge state.
        /// Reapplying the same state does not write Renderer properties again.
        /// </summary>
        public bool ApplyState(SleepingForestRootBridgeState state)
        {
            if (!HasValidConfiguration)
            {
                return false;
            }

            var isActivated =
                state == SleepingForestRootBridgeState.Activated;
            var hasChanged =
                !hasAppliedState ||
                currentState != state ||
                dormantMarker.activeSelf == isActivated ||
                activatedMarker.activeSelf != isActivated;
            SetActive(dormantMarker, !isActivated);
            SetActive(activatedMarker, isActivated);
            if (hasChanged)
            {
                ApplyColor(isActivated ? activatedColor : dormantColor);
            }

            currentState = state;
            hasAppliedState = true;
            return hasChanged;
        }

        /// <summary>
        /// Forces a single visual initialization without creating a material
        /// instance or changing the shared material asset.
        /// </summary>
        public void InitializeState()
        {
            hasAppliedState = false;
            ApplyState(SleepingForestRootBridgeState.Initial);
        }

        private void ApplyColor(Color displayColor)
        {
            materialPropertyBlock ??= new MaterialPropertyBlock();
            spanRenderer.GetPropertyBlock(materialPropertyBlock);
            materialPropertyBlock.SetColor(
                BaseColorPropertyId,
                displayColor);
            materialPropertyBlock.SetColor(
                ColorPropertyId,
                displayColor);
            spanRenderer.SetPropertyBlock(materialPropertyBlock);
        }

        private static void SetActive(GameObject marker, bool shouldBeActive)
        {
            if (marker != null && marker.activeSelf != shouldBeActive)
            {
                marker.SetActive(shouldBeActive);
            }
        }
    }
}
