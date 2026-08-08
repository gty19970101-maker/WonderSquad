using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Interaction.Detection;

namespace WonderSquad.Interaction.Execution
{
    /// <summary>
    /// Revalidates one explicit interaction target without discovering or
    /// selecting another candidate.
    /// </summary>
    public sealed class InteractionExecutionValidator
    {
        public InteractionResultCode Validate(
            in InteractionRequest request,
            IInteractable detectionTarget,
            IExecutableInteraction executableTarget,
            Transform authorityOrigin,
            InteractionSettings settings)
        {
            if (!request.Context.IsValid)
            {
                return InteractionResultCode.Unknown;
            }

            if (!request.TargetId.IsValid ||
                !IsAlive(detectionTarget))
            {
                return InteractionResultCode.TargetInvalid;
            }

            if (!IsAlive(executableTarget))
            {
                return InteractionResultCode.NotSupported;
            }

            if (authorityOrigin == null ||
                settings == null ||
                !settings.IsValid ||
                !detectionTarget.IsDetectionEnabled ||
                detectionTarget.TargetId != request.TargetId ||
                executableTarget.TargetId != request.TargetId)
            {
                return InteractionResultCode.TargetInvalid;
            }

            if (!(detectionTarget is Component targetComponent))
            {
                return InteractionResultCode.TargetInvalid;
            }

            if (!IsLayerIncluded(
                    settings.InteractionLayerMask,
                    targetComponent.gameObject.layer))
            {
                return InteractionResultCode.LayerRejected;
            }

            var originPosition = authorityOrigin.position;
            var detectionPosition = detectionTarget.DetectionPosition;
            if (!IsFinite(originPosition) ||
                !IsFinite(detectionPosition))
            {
                return InteractionResultCode.TargetInvalid;
            }

            var targetOffset = detectionPosition - originPosition;
            var maximumSquaredDistance =
                settings.DetectionRadius * settings.DetectionRadius;
            if (targetOffset.sqrMagnitude > maximumSquaredDistance)
            {
                return InteractionResultCode.OutOfRange;
            }

            if (IsOccluded(
                    originPosition,
                    targetOffset,
                    request.TargetId,
                    settings))
            {
                return InteractionResultCode.Occluded;
            }

            return executableTarget.IsExecutionAvailable
                ? InteractionResultCode.Success
                : InteractionResultCode.Busy;
        }

        private static bool IsOccluded(
            Vector3 originPosition,
            Vector3 targetOffset,
            InteractionTargetId targetId,
            InteractionSettings settings)
        {
            var targetDistance = targetOffset.magnitude;
            if (targetDistance <= Mathf.Epsilon)
            {
                return false;
            }

            if (!Physics.Raycast(
                    originPosition,
                    targetOffset / targetDistance,
                    out var hit,
                    targetDistance,
                    settings.RaycastLayerMask,
                    settings.RaycastTriggerInteraction))
            {
                return false;
            }

            var hitTarget =
                hit.collider.GetComponentInParent<IInteractable>();
            return
                !IsAlive(hitTarget) ||
                hitTarget.TargetId != targetId;
        }

        private static bool IsLayerIncluded(
            LayerMask layerMask,
            int layer)
        {
            return
                layer >= 0 &&
                layer <= 31 &&
                (layerMask.value & (1 << layer)) != 0;
        }

        private static bool IsAlive(object target)
        {
            if (target == null)
            {
                return false;
            }

            return
                !(target is Object unityObject) ||
                unityObject != null;
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
