using System;
using System.Collections.Generic;
using UnityEngine;
using WonderSquad.Core.Logging;
using WonderSquad.Player.Spawning;

namespace WonderSquad.SleepingForest.Completion
{
    /// <summary>
    /// Reports whether the explicit local player occupies the slice-end volume.
    /// It does not own or evaluate level completion.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class SleepingForestSliceEndTrigger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Scene-local source of the only accepted player instance.")]
        private PlayerSpawner playerSpawner;

        [SerializeField]
        [Tooltip("Non-blocking volume that marks the shared route destination.")]
        private BoxCollider triggerCollider;

        private readonly HashSet<Collider> presentPlayerColliders =
            new HashSet<Collider>();

        private bool isPlayerPresent;
        private bool isSpawnerSubscribed;

        public event Action<bool> PlayerPresenceChanged;

        public PlayerSpawner PlayerSpawner => playerSpawner;

        public BoxCollider TriggerCollider => triggerCollider;

        public bool IsPlayerPresent => isPlayerPresent;

        public bool HasValidConfiguration =>
            playerSpawner != null &&
            triggerCollider != null &&
            triggerCollider.isTrigger &&
            playerSpawner.gameObject.scene == gameObject.scene;

        private void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<BoxCollider>();
            }
        }

        private void OnEnable()
        {
            SubscribeToSpawner();
            ClearPresence();

            if (!HasValidConfiguration)
            {
                ProjectLog.Error(
                    "SleepingForestSliceEndTrigger requires an explicit " +
                    "PlayerSpawner and an isTrigger BoxCollider.");
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromSpawner();
            ClearPresence();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOfficialPlayerCollider(other) ||
                !presentPlayerColliders.Add(other))
            {
                return;
            }

            SetPlayerPresence(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null ||
                !presentPlayerColliders.Remove(other))
            {
                return;
            }

            RemoveDestroyedColliders();
            SetPlayerPresence(presentPlayerColliders.Count > 0);
        }

        /// <summary>
        /// Supplies the explicit scene player and trigger references before enable.
        /// </summary>
        public bool Configure(
            PlayerSpawner localPlayerSpawner,
            BoxCollider sliceEndCollider)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "SleepingForestSliceEndTrigger cannot be reconfigured " +
                    "while enabled.");
                return false;
            }

            playerSpawner = localPlayerSpawner;
            triggerCollider = sliceEndCollider;
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }

            ClearPresence();
            return HasValidConfiguration;
        }

        /// <summary>
        /// Returns true only for a Collider owned by the current spawned player.
        /// </summary>
        public bool IsOfficialPlayerCollider(Collider candidate)
        {
            if (candidate == null ||
                playerSpawner == null ||
                playerSpawner.SpawnedPlayer == null)
            {
                return false;
            }

            var player = playerSpawner.SpawnedPlayer;
            return
                candidate.gameObject == player ||
                candidate.transform.IsChildOf(player.transform);
        }

        private void HandlePlayerSpawned(GameObject player)
        {
            ClearPresence();
        }

        private void SubscribeToSpawner()
        {
            if (isSpawnerSubscribed || playerSpawner == null)
            {
                return;
            }

            playerSpawner.PlayerSpawned += HandlePlayerSpawned;
            isSpawnerSubscribed = true;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSpawnerSubscribed)
            {
                return;
            }

            if (playerSpawner != null)
            {
                playerSpawner.PlayerSpawned -= HandlePlayerSpawned;
            }

            isSpawnerSubscribed = false;
        }

        private void ClearPresence()
        {
            presentPlayerColliders.Clear();
            SetPlayerPresence(false);
        }

        private void RemoveDestroyedColliders()
        {
            presentPlayerColliders.RemoveWhere(
                collider => collider == null);
        }

        private void SetPlayerPresence(bool shouldBePresent)
        {
            if (isPlayerPresent == shouldBePresent)
            {
                return;
            }

            isPlayerPresent = shouldBePresent;
            PlayerPresenceChanged?.Invoke(isPlayerPresent);
        }
    }
}
