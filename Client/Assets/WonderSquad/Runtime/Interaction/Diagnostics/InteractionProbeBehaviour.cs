using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Identifiers;
using WonderSquad.Interaction.Detection;

namespace WonderSquad.Interaction.Diagnostics
{
    /// <summary>
    /// Standard reference interaction behavior that toggles only its local
    /// diagnostic state after an already validated execution request.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionProbeBehaviour :
        MonoBehaviour,
        IExecutableInteraction
    {
        [SerializeField]
        [Tooltip("Read-only detection identity shared with this execution behavior.")]
        private InteractionTarget interactionTarget;

        [SerializeField]
        [Tooltip("Display-only component updated after a successful state change.")]
        private InteractionProbeVisualState visualState;

        [SerializeField]
        [Tooltip("Allows this Probe to accept an already validated execution request.")]
        private bool shouldAllowExecution = true;

        private bool isActive;
        private int executionCount;
        private RequestId lastRequestId;

        public InteractionTargetId TargetId =>
            interactionTarget == null
                ? default
                : interactionTarget.TargetId;

        public bool IsExecutionAvailable =>
            shouldAllowExecution &&
            isActiveAndEnabled &&
            HasValidConfiguration &&
            interactionTarget.IsDetectionEnabled;

        public bool HasValidConfiguration =>
            interactionTarget != null &&
            interactionTarget.IsConfigurationValid &&
            visualState != null &&
            visualState.HasValidConfiguration &&
            TargetId.IsValid;

        public bool IsActive => isActive;

        public int ExecutionCount => executionCount;

        public RequestId LastRequestId => lastRequestId;

        private void Awake()
        {
            ApplyVisualState();
        }

        /// <summary>
        /// Supplies explicit composition references for isolated tests and
        /// prefab authoring validation.
        /// </summary>
        public bool Configure(
            InteractionTarget target,
            InteractionProbeVisualState probeVisualState,
            bool shouldExecute)
        {
            interactionTarget = target;
            visualState = probeVisualState;
            shouldAllowExecution = shouldExecute;
            ApplyVisualState();
            return HasValidConfiguration;
        }

        public InteractionResult Execute(in InteractionContext context)
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

            isActive = !isActive;
            executionCount++;
            lastRequestId = context.RequestId;
            ApplyVisualState();

            return new InteractionResult(
                context.RequestId,
                TargetId,
                InteractionResultCode.Success);
        }

        private void ApplyVisualState()
        {
            if (visualState == null)
            {
                return;
            }

            visualState.SetActive(isActive);
        }
    }
}
