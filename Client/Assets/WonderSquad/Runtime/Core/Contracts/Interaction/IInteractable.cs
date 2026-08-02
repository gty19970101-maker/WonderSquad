using UnityEngine;

namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Describes the read-only information required to detect an interaction
    /// target. Detection never implies that an interaction has executed.
    /// </summary>
    public interface IInteractable
    {
        InteractionTargetId TargetId { get; }

        Vector3 DetectionPosition { get; }

        bool IsDetectionEnabled { get; }

        int DetectionPriority { get; }
    }
}
