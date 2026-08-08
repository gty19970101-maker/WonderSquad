namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Defines an authority-side interaction operation independently from
    /// detection and presentation.
    /// </summary>
    public interface IExecutableInteraction
    {
        InteractionTargetId TargetId { get; }

        bool IsExecutionAvailable { get; }

        InteractionResult Execute(in InteractionContext context);
    }
}
