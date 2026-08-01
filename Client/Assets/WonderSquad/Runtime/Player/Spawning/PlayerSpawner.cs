using UnityEngine;
using UnityEngine.Serialization;

namespace WonderSquad.Player.Spawning
{
    /// <summary>
    /// Creates one local player instance for offline Sandbox validation.
    /// This component is not a network spawn authority.
    /// </summary>
    public sealed class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Local player prefab used only by the current Sandbox.")]
        private GameObject playerPrefab;

        [SerializeField]
        [Tooltip("Transform marker used for the local player spawn pose.")]
        private PlayerSpawnPoint spawnPoint;

        [FormerlySerializedAs("spawnOnStart")]
        [SerializeField]
        [Tooltip("Creates the local player when the Sandbox starts.")]
        private bool shouldSpawnOnStart = true;

        private GameObject spawnedPlayer;

        public GameObject PlayerPrefab => playerPrefab;

        public PlayerSpawnPoint SpawnPoint => spawnPoint;

        public GameObject SpawnedPlayer => spawnedPlayer;

        public bool IsConfigurationValid =>
            playerPrefab != null &&
            spawnPoint != null;

        private void Start()
        {
            if (!shouldSpawnOnStart)
            {
                return;
            }

            TrySpawn(out _);
        }

        /// <summary>
        /// Assigns the local Sandbox spawn references.
        /// </summary>
        public void Configure(
            GameObject localPlayerPrefab,
            PlayerSpawnPoint localSpawnPoint)
        {
            playerPrefab = localPlayerPrefab;
            spawnPoint = localSpawnPoint;
        }

        /// <summary>
        /// Creates a player when configuration is valid and no live instance exists.
        /// </summary>
        public bool TrySpawn(out GameObject player)
        {
            if (spawnedPlayer != null)
            {
                player = spawnedPlayer;
                return false;
            }

            if (!IsConfigurationValid)
            {
                Debug.LogError(
                    "[WonderSquad] Local PlayerSpawner requires a Player Prefab and PlayerSpawnPoint.",
                    this);
                player = null;
                return false;
            }

            spawnedPlayer = Instantiate(
                playerPrefab,
                spawnPoint.Position,
                spawnPoint.Rotation);
            player = spawnedPlayer;
            return true;
        }
    }
}
