using UnityEngine;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Level.SleepingForest.Greybox
{
    /// <summary>
    /// Returns the local greybox player to a scene-authored safe point after a fall.
    /// This temporary level component is not a general respawn or checkpoint system.
    /// </summary>
    public sealed class SleepingForestFallRecovery : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Local offline spawner used to bind the current greybox player.")]
        private PlayerSpawner playerSpawner;

        [SerializeField]
        [Tooltip("Scene-authored safe pose used after the player falls below the threshold.")]
        private Transform recoveryPoint;

        [SerializeField]
        [Tooltip("World Y below which the bound local player is returned to safety.")]
        private float fallYThreshold = -8f;

        private GameObject trackedPlayer;
        private CharacterController trackedCharacterController;
        private bool isSubscribed;
        private int recoveryCount;

        public PlayerSpawner PlayerSpawner => playerSpawner;

        public Transform RecoveryPoint => recoveryPoint;

        public float FallYThreshold => fallYThreshold;

        public GameObject TrackedPlayer => trackedPlayer;

        public int RecoveryCount => recoveryCount;

        public bool HasValidConfiguration =>
            playerSpawner != null &&
            recoveryPoint != null &&
            recoveryPoint.position.y > fallYThreshold;

        private void OnEnable()
        {
            SubscribeToSpawner();
            BindExistingPlayer();
        }

        private void Start()
        {
            if (HasValidConfiguration)
            {
                return;
            }

            Debug.LogError(
                "[WonderSquad] SleepingForestFallRecovery requires a PlayerSpawner, " +
                "a safe RecoveryPoint, and a threshold below that point.",
                this);
            enabled = false;
        }

        private void LateUpdate()
        {
            if (trackedPlayer == null ||
                trackedPlayer.transform.position.y >= fallYThreshold)
            {
                return;
            }

            RecoverTrackedPlayer();
        }

        private void OnDisable()
        {
            UnsubscribeFromSpawner();
            trackedPlayer = null;
            trackedCharacterController = null;
        }

        /// <summary>
        /// Assigns the scene-local dependencies used by the greybox recovery boundary.
        /// </summary>
        public void Configure(
            PlayerSpawner localPlayerSpawner,
            Transform safeRecoveryPoint,
            float yThreshold)
        {
            UnsubscribeFromSpawner();
            playerSpawner = localPlayerSpawner;
            recoveryPoint = safeRecoveryPoint;
            fallYThreshold = yThreshold;
            SubscribeToSpawner();
            BindExistingPlayer();
        }

        private void SubscribeToSpawner()
        {
            if (!isActiveAndEnabled || playerSpawner == null || isSubscribed)
            {
                return;
            }

            playerSpawner.PlayerSpawned += OnPlayerSpawned;
            isSubscribed = true;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSubscribed || playerSpawner == null)
            {
                isSubscribed = false;
                return;
            }

            playerSpawner.PlayerSpawned -= OnPlayerSpawned;
            isSubscribed = false;
        }

        private void BindExistingPlayer()
        {
            if (playerSpawner == null || playerSpawner.SpawnedPlayer == null)
            {
                return;
            }

            BindPlayer(playerSpawner.SpawnedPlayer);
        }

        private void OnPlayerSpawned(GameObject player)
        {
            BindPlayer(player);
        }

        private void BindPlayer(GameObject player)
        {
            trackedPlayer = player;
            trackedCharacterController =
                player != null
                    ? player.GetComponent<CharacterController>()
                    : null;
        }

        private void RecoverTrackedPlayer()
        {
            if (!HasValidConfiguration || trackedPlayer == null)
            {
                return;
            }

            var wasControllerEnabled =
                trackedCharacterController != null &&
                trackedCharacterController.enabled;
            if (wasControllerEnabled)
            {
                trackedCharacterController.enabled = false;
            }

            trackedPlayer.transform.SetPositionAndRotation(
                recoveryPoint.position,
                recoveryPoint.rotation);

            if (wasControllerEnabled)
            {
                trackedCharacterController.enabled = true;
            }

            recoveryCount++;
        }
    }
}
