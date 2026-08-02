namespace WonderSquad.Interaction.Prompt
{
    /// <summary>
    /// Exposes static, read-only prompt semantics for a detected target.
    /// </summary>
    public interface IInteractionPromptSource
    {
        InteractionPromptDefinition Definition { get; }

        bool HasValidPromptConfiguration { get; }
    }
}
