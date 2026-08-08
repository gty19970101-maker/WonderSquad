using WonderSquad.Core.Identifiers;

namespace WonderSquad.Core.Contracts.Player
{
    /// <summary>
    /// Provides the stable identity of a player that can issue gameplay
    /// requests.
    /// </summary>
    public interface IPlayerIdentitySource
    {
        PlayerId PlayerId { get; }

        bool HasValidIdentity { get; }
    }
}
