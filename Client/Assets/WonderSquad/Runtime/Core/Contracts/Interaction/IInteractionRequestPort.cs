namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Applies a validated interaction request through a replaceable
    /// authority boundary.
    /// </summary>
    public interface IInteractionRequestPort
    {
        bool IsAvailable { get; }

        InteractionResult Execute(
            in InteractionRequest request,
            IExecutableInteraction target);
    }
}
