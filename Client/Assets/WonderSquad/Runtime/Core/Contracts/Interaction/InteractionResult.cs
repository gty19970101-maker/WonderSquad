using WonderSquad.Core.Identifiers;

namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Correlates a structured interaction outcome with its request and
    /// target.
    /// </summary>
    public readonly struct InteractionResult
    {
        public InteractionResult(
            RequestId requestId,
            InteractionTargetId targetId,
            InteractionResultCode code)
        {
            RequestId = requestId;
            TargetId = targetId;
            Code = code;
        }

        public RequestId RequestId { get; }

        public InteractionTargetId TargetId { get; }

        public InteractionResultCode Code { get; }

        public bool IsSuccess => Code == InteractionResultCode.Success;

        public static InteractionResult Create(
            in InteractionRequest request,
            InteractionResultCode code)
        {
            return new InteractionResult(
                request.Context.RequestId,
                request.TargetId,
                code);
        }
    }
}
