using UnityEngine;

namespace WonderSquad.Interaction.Prompt
{
    /// <summary>
    /// Connects a target prefab to static prompt semantics without executing
    /// interaction behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionPromptSource :
        MonoBehaviour,
        IInteractionPromptSource
    {
        [SerializeField]
        [Tooltip("Static prompt definition displayed for this target.")]
        private InteractionPromptDefinition promptDefinition;

        public InteractionPromptDefinition Definition =>
            promptDefinition;

        public bool HasValidPromptConfiguration =>
            promptDefinition != null &&
            promptDefinition.IsValid;

        /// <summary>
        /// Supplies the static definition during explicit composition.
        /// </summary>
        public bool Configure(InteractionPromptDefinition definition)
        {
            promptDefinition = definition;
            return HasValidPromptConfiguration;
        }
    }
}
