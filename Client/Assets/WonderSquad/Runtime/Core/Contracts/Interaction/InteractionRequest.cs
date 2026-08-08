namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Targets an immutable interaction context at one stable interaction ID.
    /// </summary>
    public readonly struct InteractionRequest
    {
        public InteractionRequest(
            InteractionContext context,
            InteractionTargetId targetId)
        {
            Context = context;
            TargetId = targetId;
        }

        public InteractionContext Context { get; }

        public InteractionTargetId TargetId { get; }

        public bool IsValid => Context.IsValid && TargetId.IsValid;
    }
}
