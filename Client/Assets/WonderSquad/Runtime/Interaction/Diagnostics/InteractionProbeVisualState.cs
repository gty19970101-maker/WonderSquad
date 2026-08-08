using UnityEngine;

namespace WonderSquad.Interaction.Diagnostics
{
    /// <summary>
    /// Maps one explicitly supplied Probe state to minimal local visual
    /// feedback. It never decides or changes interaction state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionProbeVisualState : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId =
            Shader.PropertyToID("_Color");

        [SerializeField]
        [Tooltip("Renderer used only for the Probe's local state feedback.")]
        private Renderer placeholderRenderer;

        [SerializeField]
        [Tooltip("Display color used while the Probe is inactive.")]
        private Color inactiveColor = Color.white;

        [SerializeField]
        [Tooltip("Display color used while the Probe is active.")]
        private Color activeColor = Color.green;

        private bool isDisplayingActive;
        private MaterialPropertyBlock colorPropertyBlock;

        public bool IsDisplayingActive => isDisplayingActive;

        public bool HasValidConfiguration => placeholderRenderer != null;

        private void Awake()
        {
            ApplyVisualState();
        }

        /// <summary>
        /// Supplies explicit visual references for prefab composition and
        /// isolated tests. This component remains display-only.
        /// </summary>
        public bool Configure(
            Renderer renderer,
            Color inactiveStateColor,
            Color activeStateColor)
        {
            placeholderRenderer = renderer;
            inactiveColor = inactiveStateColor;
            activeColor = activeStateColor;
            ApplyVisualState();
            return HasValidConfiguration;
        }

        /// <summary>
        /// Updates local presentation after the owning Probe has already
        /// accepted an interaction result.
        /// </summary>
        public bool SetActive(bool shouldDisplayActive)
        {
            if (!HasValidConfiguration)
            {
                return false;
            }

            isDisplayingActive = shouldDisplayActive;
            ApplyVisualState();
            return true;
        }

        private void ApplyVisualState()
        {
            if (placeholderRenderer == null)
            {
                return;
            }

            if (colorPropertyBlock == null)
            {
                colorPropertyBlock = new MaterialPropertyBlock();
            }

            var displayColor =
                isDisplayingActive
                    ? activeColor
                    : inactiveColor;
            placeholderRenderer.GetPropertyBlock(colorPropertyBlock);
            colorPropertyBlock.SetColor(BaseColorPropertyId, displayColor);
            colorPropertyBlock.SetColor(ColorPropertyId, displayColor);
            placeholderRenderer.SetPropertyBlock(colorPropertyBlock);
        }
    }
}
