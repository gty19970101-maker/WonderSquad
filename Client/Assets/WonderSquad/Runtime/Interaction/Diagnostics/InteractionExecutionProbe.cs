using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;

namespace WonderSquad.Interaction.Diagnostics
{
    /// <summary>
    /// Sandbox-only interaction target that records execution evidence. It is
    /// not a gameplay mechanism or a base class for future mechanisms.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionExecutionProbe :
        MonoBehaviour,
        IInteractable,
        IExecutableInteraction
    {
        [SerializeField]
        [Tooltip("Stable identifier shared by detection and execution.")]
        private string targetId = string.Empty;

        [SerializeField]
        [Tooltip("Point used for detection and execution revalidation.")]
        private Transform detectionAnchor;

        [SerializeField]
        [Tooltip("Allows this Probe to participate in local detection.")]
        private bool shouldAllowDetection = true;

        [SerializeField]
        [Tooltip("Allows the local execution port to invoke this Probe.")]
        private bool shouldAllowExecution = true;

        [SerializeField]
        [Tooltip("Higher values win before distance during detection.")]
        private int detectionPriority;

        [SerializeField]
        [Tooltip("Structured result returned when this Probe is invoked.")]
        private InteractionResultCode configuredResultCode =
            InteractionResultCode.Success;

        [SerializeField]
        [Tooltip("Optional placeholder renderer toggled after successful execution.")]
        private Renderer placeholderRenderer;

        [SerializeField]
        private Color idleColor = Color.white;

        [SerializeField]
        private Color executedColor = Color.green;

        private int executionCount;
        private bool isProbeStateActive;
        private RequestId lastRequestId;
        private PlayerId lastPlayerId;

        public InteractionTargetId TargetId =>
            new InteractionTargetId(targetId);

        public Vector3 DetectionPosition =>
            detectionAnchor == null
                ? transform.position
                : detectionAnchor.position;

        public bool IsDetectionEnabled =>
            shouldAllowDetection &&
            isActiveAndEnabled &&
            HasValidConfiguration;

        public int DetectionPriority => detectionPriority;

        public bool IsExecutionAvailable =>
            shouldAllowExecution &&
            isActiveAndEnabled &&
            HasValidConfiguration;

        public bool HasValidConfiguration =>
            TargetId.IsValid &&
            detectionAnchor != null;

        public int ExecutionCount => executionCount;

        public bool IsProbeStateActive => isProbeStateActive;

        public RequestId LastRequestId => lastRequestId;

        public PlayerId LastPlayerId => lastPlayerId;

        public InteractionResultCode ConfiguredResultCode =>
            configuredResultCode;

        private void Awake()
        {
            ResolveReferences();
            ApplyPlaceholderColor();
        }

        /// <summary>
        /// Supplies diagnostic target data for isolated tests.
        /// </summary>
        public bool Configure(
            string stableTargetId,
            Transform anchor,
            bool shouldDetect,
            bool shouldExecute,
            int priority,
            InteractionResultCode resultCode)
        {
            targetId = stableTargetId ?? string.Empty;
            detectionAnchor = anchor;
            shouldAllowDetection = shouldDetect;
            shouldAllowExecution = shouldExecute;
            detectionPriority = priority;
            configuredResultCode = resultCode;
            ResolveReferences();
            return HasValidConfiguration;
        }

        public InteractionResult Execute(
            in InteractionContext context)
        {
            if (!context.IsValid)
            {
                return new InteractionResult(
                    context.RequestId,
                    TargetId,
                    InteractionResultCode.Unknown);
            }

            if (!IsExecutionAvailable)
            {
                return new InteractionResult(
                    context.RequestId,
                    TargetId,
                    InteractionResultCode.Busy);
            }

            lastRequestId = context.RequestId;
            lastPlayerId = context.PlayerId;

            if (configuredResultCode ==
                InteractionResultCode.Success)
            {
                executionCount++;
                isProbeStateActive = !isProbeStateActive;
                ApplyPlaceholderColor();
            }

            return new InteractionResult(
                context.RequestId,
                TargetId,
                configuredResultCode);
        }

        private void ResolveReferences()
        {
            if (detectionAnchor == null)
            {
                detectionAnchor = transform;
            }

            if (placeholderRenderer == null)
            {
                placeholderRenderer =
                    GetComponentInChildren<Renderer>();
            }
        }

        private void ApplyPlaceholderColor()
        {
            if (placeholderRenderer == null)
            {
                return;
            }

            placeholderRenderer.material.color =
                isProbeStateActive
                    ? executedColor
                    : idleColor;
        }
    }
}
