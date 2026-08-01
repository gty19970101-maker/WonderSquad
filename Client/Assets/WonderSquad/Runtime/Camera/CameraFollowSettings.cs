using UnityEngine;

namespace WonderSquad.Presentation.Camera
{
    /// <summary>
    /// Stores the tunable parameters for the controlled local follow camera.
    /// Runtime camera state must not be written back to this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CameraFollowSettings",
        menuName = "Wonder Squad/Camera/Follow Settings")]
    public sealed class CameraFollowSettings : ScriptableObject
    {
        private const float MaximumDamping = 20f;
        private const float MinimumFieldOfView = 1f;
        private const float MaximumFieldOfView = 179f;
        private const float MinimumPitch = 1f;
        private const float MaximumPitch = 89f;

        [SerializeField]
        [Tooltip("Local position of the camera anchor beneath the Player root.")]
        private Vector3 targetLocalOffset = new Vector3(0f, 1.5f, 0f);

        [SerializeField]
        [Tooltip("World-space offset maintained from the camera target.")]
        private Vector3 followOffset = new Vector3(0f, 6.5f, -9f);

        [SerializeField]
        [Tooltip("Per-axis position damping used by Cinemachine Follow.")]
        private Vector3 positionDamping =
            new Vector3(0.35f, 0.5f, 0.35f);

        [SerializeField]
        [Tooltip("Fixed world-space camera rotation in degrees.")]
        private Vector3 fixedEulerAngles = new Vector3(32f, 0f, 0f);

        [SerializeField]
        [Range(MinimumFieldOfView, MaximumFieldOfView)]
        [Tooltip("Perspective field of view used by the controlled camera.")]
        private float fieldOfView = 60f;

        public Vector3 TargetLocalOffset => targetLocalOffset;

        public Vector3 FollowOffset => followOffset;

        public Vector3 PositionDamping => positionDamping;

        public Vector3 FixedEulerAngles => fixedEulerAngles;

        public float FieldOfView => fieldOfView;

        public bool IsValid =>
            IsFinite(targetLocalOffset) &&
            IsFinite(followOffset) &&
            followOffset.sqrMagnitude > 0f &&
            IsValidDamping(positionDamping) &&
            IsFinite(fixedEulerAngles) &&
            fixedEulerAngles.x >= MinimumPitch &&
            fixedEulerAngles.x <= MaximumPitch &&
            Mathf.Approximately(fixedEulerAngles.z, 0f) &&
            IsFinite(fieldOfView) &&
            fieldOfView >= MinimumFieldOfView &&
            fieldOfView <= MaximumFieldOfView;

        public static bool IsValidDamping(Vector3 damping)
        {
            return
                IsFinite(damping) &&
                damping.x >= 0f &&
                damping.y >= 0f &&
                damping.z >= 0f &&
                damping.x <= MaximumDamping &&
                damping.y <= MaximumDamping &&
                damping.z <= MaximumDamping;
        }

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
    }
}
