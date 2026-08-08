using UnityEngine;
using WonderSquad.Core.Identifiers;

namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Describes who requested an interaction and the immutable local intent
    /// snapshot used by an authority validator.
    /// </summary>
    public readonly struct InteractionContext
    {
        public InteractionContext(
            PlayerId playerId,
            RequestId requestId,
            double requestTimeSeconds,
            Vector3 interactionOrigin,
            Vector3 interactionDirection,
            uint expectedTargetRevision = 0U)
        {
            PlayerId = playerId;
            RequestId = requestId;
            RequestTimeSeconds = requestTimeSeconds;
            InteractionOrigin = interactionOrigin;
            InteractionDirection =
                IsFinite(interactionDirection) &&
                interactionDirection.sqrMagnitude > Mathf.Epsilon
                    ? interactionDirection.normalized
                    : Vector3.zero;
            ExpectedTargetRevision = expectedTargetRevision;
        }

        public PlayerId PlayerId { get; }

        public RequestId RequestId { get; }

        public double RequestTimeSeconds { get; }

        public Vector3 InteractionOrigin { get; }

        public Vector3 InteractionDirection { get; }

        public uint ExpectedTargetRevision { get; }

        public bool IsValid =>
            PlayerId.IsValid &&
            RequestId.IsValid &&
            IsFinite(RequestTimeSeconds) &&
            RequestTimeSeconds >= 0d &&
            IsFinite(InteractionOrigin) &&
            IsFinite(InteractionDirection) &&
            InteractionDirection.sqrMagnitude > Mathf.Epsilon;

        private static bool IsFinite(Vector3 value)
        {
            return
                IsFinite(value.x) &&
                IsFinite(value.y) &&
                IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
