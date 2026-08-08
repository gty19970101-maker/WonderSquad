using UnityEngine;
using WonderSquad.Core.Contracts.Player;
using WonderSquad.Core.Identifiers;

namespace WonderSquad.Player.Identity
{
    /// <summary>
    /// Provides an explicit offline player identity for Sandbox requests.
    /// Network spawning will replace this source in a later Sprint.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LocalPlayerIdentity :
        MonoBehaviour,
        IPlayerIdentitySource
    {
        [SerializeField]
        [Tooltip("Non-zero player ID used by the offline Sandbox only.")]
        private ulong playerIdValue = 1UL;

        public PlayerId PlayerId => new PlayerId(playerIdValue);

        public bool HasValidIdentity => PlayerId.IsValid;

        /// <summary>
        /// Supplies an explicit offline identity for isolated tests.
        /// </summary>
        public bool Configure(ulong value)
        {
            playerIdValue = value;
            return HasValidIdentity;
        }
    }
}
