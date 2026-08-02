using UnityEngine;

namespace WonderSquad.Interaction.Prompt
{
    /// <summary>
    /// Stores static prompt semantics. Localized text resolution remains a
    /// presentation concern and runtime visibility is never stored here.
    /// </summary>
    [CreateAssetMenu(
        fileName = "InteractionPromptDefinition",
        menuName = "Wonder Squad/Interaction/Prompt Definition")]
    public sealed class InteractionPromptDefinition : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Stable prompt identifier independent of scene object names.")]
        private string promptId = string.Empty;

        [SerializeField]
        [Tooltip("Stable key reserved for a future localization resolver.")]
        private string actionTextKey = string.Empty;

        [SerializeField]
        [Tooltip("Development text used until localization is implemented.")]
        private string fallbackActionText = string.Empty;

        public string PromptId => promptId;

        public string ActionTextKey => actionTextKey;

        public string FallbackActionText => fallbackActionText;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(promptId) &&
            !string.IsNullOrWhiteSpace(actionTextKey) &&
            !string.IsNullOrWhiteSpace(fallbackActionText);

        /// <summary>
        /// Supplies static authoring data before the definition is consumed.
        /// Runtime systems must treat the configured definition as read-only.
        /// </summary>
        public bool Configure(
            string stablePromptId,
            string localizationKey,
            string fallbackText)
        {
            promptId = stablePromptId ?? string.Empty;
            actionTextKey = localizationKey ?? string.Empty;
            fallbackActionText = fallbackText ?? string.Empty;
            return IsValid;
        }
    }
}
