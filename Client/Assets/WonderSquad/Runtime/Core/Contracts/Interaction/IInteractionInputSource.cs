using System;

namespace WonderSquad.Core.Contracts.Interaction
{
    /// <summary>
    /// Publishes local primary-interaction input edges without exposing an
    /// input device or Input System type.
    /// </summary>
    public interface IInteractionInputSource
    {
        event Action InteractionPressed;

        bool IsInputAvailable { get; }

        bool IsInteractionHeld { get; }
    }
}
