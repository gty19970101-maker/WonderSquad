namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Stable, non-localized result of an interaction request.
    /// </summary>
    public enum InteractionResultCode
    {
        Unknown = 0,
        Success = 1,
        TargetInvalid = 2,
        OutOfRange = 3,
        Occluded = 4,
        LayerRejected = 5,
        Busy = 6,
        Cancelled = 7,
        NotSupported = 8
    }
}
