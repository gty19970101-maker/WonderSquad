using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;
using WonderSquad.Core.Logging;
using WonderSquad.Player.Camera;
using WonderSquad.Player.Spawning;

namespace WonderSquad.Presentation.Camera
{
    /// <summary>
    /// Binds the current local Sandbox player target to a Cinemachine camera.
    /// Cinemachine owns follow motion; this component owns only configuration
    /// and explicit local-player target selection.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    public sealed class CameraTargetBinder : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit local Sandbox player source.")]
        private PlayerSpawner playerSpawner;

        [SerializeField]
        [Tooltip("Cinemachine camera receiving the local player target.")]
        private CinemachineCamera controlledCamera;

        [SerializeField]
        [Tooltip("Cinemachine component responsible for follow motion.")]
        private CinemachineFollow controlledFollow;

        [SerializeField]
        [Tooltip("Data asset containing all controlled follow parameters.")]
        private CameraFollowSettings followSettings;

        private GameObject boundPlayer;
        private PlayerCameraTarget boundTarget;
        private bool isSubscribed;

        public GameObject BoundPlayer => boundPlayer;

        public PlayerCameraTarget BoundTarget => boundTarget;

        public Transform TrackingTarget =>
            controlledCamera == null ? null : controlledCamera.Follow;

        public bool IsBound =>
            boundPlayer != null &&
            boundTarget != null &&
            TrackingTarget == boundTarget.TargetTransform;

        public bool HasValidRigConfiguration =>
            controlledCamera != null &&
            controlledFollow != null &&
            followSettings != null &&
            followSettings.IsValid;

        public bool HasValidConfiguration =>
            playerSpawner != null &&
            HasValidRigConfiguration;

        private void Awake()
        {
            ResolveLocalReferences();
            ApplySettings();
        }

        private void OnEnable()
        {
            ResolveLocalReferences();
            if (!HasValidRigConfiguration)
            {
                ProjectLog.Error(
                    "CameraTargetBinder requires a Cinemachine camera, " +
                    "Cinemachine Follow, and valid CameraFollowSettings.");
                enabled = false;
                return;
            }

            SubscribeToSpawner();
            TryBindCurrentPlayer();
        }

        private void OnDisable()
        {
            UnsubscribeFromSpawner();
            ClearBinding();
        }

        private void LateUpdate()
        {
            if (boundPlayer == null || boundTarget == null)
            {
                ClearBinding();
                TryBindCurrentPlayer();
                return;
            }

            if (playerSpawner != null &&
                playerSpawner.SpawnedPlayer != boundPlayer)
            {
                TryBind(playerSpawner.SpawnedPlayer);
            }
        }

        /// <summary>
        /// Assigns the scene-local source and Cinemachine rig references.
        /// </summary>
        public void Configure(
            PlayerSpawner localPlayerSpawner,
            CinemachineCamera localControlledCamera,
            CinemachineFollow localControlledFollow,
            CameraFollowSettings settings)
        {
            UnsubscribeFromSpawner();
            playerSpawner = localPlayerSpawner;
            controlledCamera = localControlledCamera;
            controlledFollow = localControlledFollow;
            followSettings = settings;
            ResolveLocalReferences();
            ApplySettings();

            if (isActiveAndEnabled)
            {
                SubscribeToSpawner();
                TryBindCurrentPlayer();
            }
        }

        /// <summary>
        /// Binds an explicitly selected local player without scene-name lookup.
        /// </summary>
        public bool TryBind(GameObject localPlayer)
        {
            if (!HasValidRigConfiguration || localPlayer == null)
            {
                ClearBinding();
                return false;
            }

            var cameraTarget =
                localPlayer.GetComponentInChildren<PlayerCameraTarget>(true);
            if (cameraTarget == null ||
                !cameraTarget.IsConfigurationValid)
            {
                ClearBinding();
                return false;
            }

            boundPlayer = localPlayer;
            boundTarget = cameraTarget;
            boundTarget.TargetTransform.localPosition =
                followSettings.TargetLocalOffset;
            controlledCamera.Follow = boundTarget.TargetTransform;
            controlledCamera.PreviousStateIsValid = false;
            return true;
        }

        private void TryBindCurrentPlayer()
        {
            if (playerSpawner == null)
            {
                return;
            }

            TryBind(playerSpawner.SpawnedPlayer);
        }

        private void ApplySettings()
        {
            if (!HasValidRigConfiguration)
            {
                return;
            }

            var trackerSettings = controlledFollow.TrackerSettings;
            trackerSettings.BindingMode = BindingMode.WorldSpace;
            trackerSettings.PositionDamping =
                followSettings.PositionDamping;
            trackerSettings.RotationDamping = Vector3.zero;
            trackerSettings.QuaternionDamping = 0f;
            controlledFollow.TrackerSettings = trackerSettings;
            controlledFollow.FollowOffset = followSettings.FollowOffset;

            controlledCamera.transform.rotation =
                Quaternion.Euler(followSettings.FixedEulerAngles);
            var lens = controlledCamera.Lens;
            lens.FieldOfView = followSettings.FieldOfView;
            controlledCamera.Lens = lens;
            controlledCamera.PreviousStateIsValid = false;
        }

        private void SubscribeToSpawner()
        {
            if (isSubscribed || playerSpawner == null)
            {
                return;
            }

            playerSpawner.PlayerSpawned += HandlePlayerSpawned;
            isSubscribed = true;
        }

        private void UnsubscribeFromSpawner()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (playerSpawner != null)
            {
                playerSpawner.PlayerSpawned -= HandlePlayerSpawned;
            }

            isSubscribed = false;
        }

        private void HandlePlayerSpawned(GameObject player)
        {
            TryBind(player);
        }

        private void ClearBinding()
        {
            if (controlledCamera != null)
            {
                controlledCamera.Follow = null;
                controlledCamera.PreviousStateIsValid = false;
            }

            boundPlayer = null;
            boundTarget = null;
        }

        private void ResolveLocalReferences()
        {
            if (controlledCamera == null)
            {
                controlledCamera = GetComponent<CinemachineCamera>();
            }

            if (controlledFollow == null)
            {
                controlledFollow = GetComponent<CinemachineFollow>();
            }
        }
    }
}
