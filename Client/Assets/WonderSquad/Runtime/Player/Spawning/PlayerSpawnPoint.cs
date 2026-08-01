using UnityEngine;

namespace WonderSquad.Player.Spawning
{
    /// <summary>
    /// Marks a local Sandbox position and rotation where a player can spawn.
    /// </summary>
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        public Vector3 Position => transform.position;

        public Quaternion Rotation => transform.rotation;
    }
}
