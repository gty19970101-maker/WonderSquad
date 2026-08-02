using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Interaction.Detection
{
    /// <summary>
    /// Adapts a Unity scene object to the read-only interaction target
    /// contract. It does not execute interaction behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionTarget : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [Tooltip("Stable identifier used for deterministic target selection.")]
        private string targetId = string.Empty;

        [SerializeField]
        [Tooltip("Point used for distance and line-of-sight checks.")]
        private Transform detectionAnchor;

        [SerializeField]
        [Tooltip("Allows this target to participate in local detection.")]
        private bool shouldAllowDetection = true;

        [SerializeField]
        [Tooltip("Higher values win before distance when selecting a target.")]
        private int detectionPriority;

        public InteractionTargetId TargetId =>
            new InteractionTargetId(targetId);

        public Vector3 DetectionPosition =>
            detectionAnchor == null
                ? transform.position
                : detectionAnchor.position;

        public bool IsDetectionEnabled =>
            shouldAllowDetection &&
            isActiveAndEnabled &&
            IsConfigurationValid;

        public int DetectionPriority => detectionPriority;

        public bool IsConfigurationValid =>
            TargetId.IsValid &&
            detectionAnchor != null;

        private void Awake()
        {
            ResolveDetectionAnchor();
        }

        /// <summary>
        /// Supplies the read-only target data used by scene composition and
        /// isolated tests.
        /// </summary>
        public bool Configure(
            string stableTargetId,
            Transform anchor,
            bool shouldAllow,
            int priority)
        {
            targetId = stableTargetId ?? string.Empty;
            detectionAnchor = anchor;
            shouldAllowDetection = shouldAllow;
            detectionPriority = priority;
            ResolveDetectionAnchor();
            return IsConfigurationValid;
        }

        private void ResolveDetectionAnchor()
        {
            if (detectionAnchor == null)
            {
                detectionAnchor = transform;
            }
        }
    }
}
