using System;
using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;
using WonderSquad.Core.Logging;

namespace WonderSquad.Interaction.Detection
{
    /// <summary>
    /// Discovers and selects a local interaction target without reading input
    /// or executing interaction behavior.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InteractionDetector : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit player-space origin for range and occlusion queries.")]
        private Transform detectionOrigin;

        [SerializeField]
        [Tooltip("Data asset containing all interaction detection parameters.")]
        private InteractionSettings interactionSettings;

        [SerializeField]
        [Tooltip("Runtime-only component shown for Sandbox inspection.")]
        private MonoBehaviour currentTargetComponent;

        private readonly InteractionValidator validator =
            new InteractionValidator();

        private Collider[] candidateColliders;
        private InteractionTargetId[] processedTargetIds;
        private IInteractable currentTarget;
        private bool hasReportedBufferSaturation;

        public event Action<IInteractable> CurrentTargetChanged;

        public Transform DetectionOrigin => detectionOrigin;

        public InteractionSettings Settings => interactionSettings;

        public IInteractable CurrentTarget =>
            IsAlive(currentTarget) ? currentTarget : null;

        public bool HasCurrentTarget => CurrentTarget != null;

        public int CandidateBufferCapacity =>
            candidateColliders == null
                ? 0
                : candidateColliders.Length;

        public bool HasValidConfiguration =>
            detectionOrigin != null &&
            interactionSettings != null &&
            interactionSettings.IsValid;

        private void OnEnable()
        {
            if (!TryInitialize())
            {
                ProjectLog.Error(
                    "InteractionDetector requires a detection origin and valid InteractionSettings.");
                enabled = false;
            }
        }

        private void OnDisable()
        {
            ClearCurrentTarget();
        }

        private void Update()
        {
            RefreshDetection();
        }

        /// <summary>
        /// Supplies the local origin and static settings before detection.
        /// </summary>
        public bool Configure(
            Transform origin,
            InteractionSettings settings)
        {
            detectionOrigin = origin;
            interactionSettings = settings;
            ClearCurrentTarget();
            return TryInitialize();
        }

        /// <summary>
        /// Refreshes the local candidate selection without executing an
        /// interaction or changing any target state.
        /// </summary>
        public bool RefreshDetection()
        {
            if (!TryInitialize())
            {
                ClearCurrentTarget();
                return false;
            }

            var colliderCount = Physics.OverlapSphereNonAlloc(
                detectionOrigin.position,
                interactionSettings.DetectionRadius,
                candidateColliders,
                interactionSettings.InteractionLayerMask,
                interactionSettings.RangeQueryTriggerInteraction);

            if (colliderCount >= candidateColliders.Length &&
                !hasReportedBufferSaturation)
            {
                ProjectLog.Warning(
                    "InteractionDetector candidate buffer is full. " +
                    "Increase InteractionSettings Max Candidate Count.");
                hasReportedBufferSaturation = true;
            }

            var originPosition = detectionOrigin.position;
            var processedTargetCount = 0;
            IInteractable bestTarget = null;
            var bestPriority = int.MinValue;
            var bestSquaredDistance = float.PositiveInfinity;
            var bestTargetId = default(InteractionTargetId);

            for (var index = 0; index < colliderCount; index++)
            {
                var candidateCollider = candidateColliders[index];
                if (candidateCollider == null ||
                    !candidateCollider.enabled ||
                    !candidateCollider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var candidate =
                    candidateCollider.GetComponentInParent<IInteractable>();
                if (!IsAlive(candidate) ||
                    !candidate.TargetId.IsValid ||
                    ContainsProcessedTarget(
                        candidate.TargetId,
                        processedTargetCount))
                {
                    continue;
                }

                processedTargetIds[processedTargetCount] =
                    candidate.TargetId;
                processedTargetCount++;

                var isOccluded = IsOccluded(candidate);
                if (!validator.TryValidate(
                        candidate,
                        candidateCollider.gameObject.layer,
                        originPosition,
                        interactionSettings,
                        isOccluded,
                        out var squaredDistance))
                {
                    continue;
                }

                if (!IsBetterCandidate(
                        candidate,
                        squaredDistance,
                        bestTarget,
                        bestPriority,
                        bestSquaredDistance,
                        bestTargetId))
                {
                    continue;
                }

                bestTarget = candidate;
                bestPriority = candidate.DetectionPriority;
                bestSquaredDistance = squaredDistance;
                bestTargetId = candidate.TargetId;
            }

            SetCurrentTarget(bestTarget);
            return bestTarget != null;
        }

        private bool TryInitialize()
        {
            if (!HasValidConfiguration)
            {
                return false;
            }

            var requiredCapacity =
                interactionSettings.MaxCandidateCount;
            if (candidateColliders == null ||
                candidateColliders.Length != requiredCapacity)
            {
                candidateColliders =
                    new Collider[requiredCapacity];
                processedTargetIds =
                    new InteractionTargetId[requiredCapacity];
                hasReportedBufferSaturation = false;
            }

            return true;
        }

        private bool ContainsProcessedTarget(
            InteractionTargetId targetId,
            int processedTargetCount)
        {
            for (var index = 0;
                 index < processedTargetCount;
                 index++)
            {
                if (processedTargetIds[index] == targetId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsOccluded(IInteractable candidate)
        {
            var originPosition = detectionOrigin.position;
            var targetOffset =
                candidate.DetectionPosition - originPosition;
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
                    interactionSettings.RaycastLayerMask,
                    interactionSettings.RaycastTriggerInteraction))
            {
                return false;
            }

            var hitTarget =
                hit.collider.GetComponentInParent<IInteractable>();
            return
                !IsAlive(hitTarget) ||
                hitTarget.TargetId != candidate.TargetId;
        }

        private static bool IsBetterCandidate(
            IInteractable candidate,
            float squaredDistance,
            IInteractable bestTarget,
            int bestPriority,
            float bestSquaredDistance,
            InteractionTargetId bestTargetId)
        {
            if (bestTarget == null)
            {
                return true;
            }

            if (candidate.DetectionPriority != bestPriority)
            {
                return candidate.DetectionPriority > bestPriority;
            }

            if (!Mathf.Approximately(
                    squaredDistance,
                    bestSquaredDistance))
            {
                return squaredDistance < bestSquaredDistance;
            }

            return candidate.TargetId.CompareTo(bestTargetId) < 0;
        }

        private void SetCurrentTarget(IInteractable target)
        {
            if (ReferenceEquals(currentTarget, target))
            {
                return;
            }

            currentTarget = target;
            currentTargetComponent = target as MonoBehaviour;
            CurrentTargetChanged?.Invoke(CurrentTarget);
        }

        private void ClearCurrentTarget()
        {
            SetCurrentTarget(null);
        }

        private static bool IsAlive(IInteractable target)
        {
            if (target == null)
            {
                return false;
            }

            return
                !(target is UnityEngine.Object unityObject) ||
                unityObject != null;
        }
    }
}
