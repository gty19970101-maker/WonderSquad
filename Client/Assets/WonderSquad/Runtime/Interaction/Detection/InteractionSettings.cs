using UnityEngine;

namespace WonderSquad.Interaction.Detection
{
    /// <summary>
    /// Stores static parameters for local interaction target detection.
    /// Runtime candidates and focus state must not be written to this asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "InteractionSettings",
        menuName = "Wonder Squad/Interaction/Detection Settings")]
    public sealed class InteractionSettings : ScriptableObject
    {
        private const float DefaultDetectionRadiusMeters = 2.75f;
        private const int DefaultMaxCandidateCount = 16;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Maximum target detection distance in world units.")]
        private float detectionRadius =
            DefaultDetectionRadiusMeters;

        [SerializeField]
        [Tooltip("Physics layers that can contain interaction targets.")]
        private LayerMask interactionLayerMask = ~0;

        [SerializeField]
        [Min(1)]
        [Tooltip("Fixed collider buffer capacity used by the non-allocating query.")]
        private int maxCandidateCount =
            DefaultMaxCandidateCount;

        [SerializeField]
        [Tooltip("Whether the range query includes trigger colliders.")]
        private QueryTriggerInteraction rangeQueryTriggerInteraction =
            QueryTriggerInteraction.Collide;

        [SerializeField]
        [Tooltip("Physics layers that can block line of sight to a target.")]
        private LayerMask raycastLayerMask = ~0;

        [SerializeField]
        [Tooltip("Whether the occlusion ray checks trigger colliders.")]
        private QueryTriggerInteraction raycastTriggerInteraction =
            QueryTriggerInteraction.Ignore;

        public float DetectionRadius => detectionRadius;

        public LayerMask InteractionLayerMask => interactionLayerMask;

        public int MaxCandidateCount => maxCandidateCount;

        public QueryTriggerInteraction RangeQueryTriggerInteraction =>
            rangeQueryTriggerInteraction;

        public LayerMask RaycastLayerMask => raycastLayerMask;

        public QueryTriggerInteraction RaycastTriggerInteraction =>
            raycastTriggerInteraction;

        public bool IsValid =>
            IsFinite(detectionRadius) &&
            detectionRadius > 0f &&
            interactionLayerMask.value != 0 &&
            maxCandidateCount > 0 &&
            IsValidQueryTriggerInteraction(
                rangeQueryTriggerInteraction) &&
            raycastLayerMask.value != 0 &&
            IsValidQueryTriggerInteraction(
                raycastTriggerInteraction);

        private static bool IsValidQueryTriggerInteraction(
            QueryTriggerInteraction value)
        {
            return
                value >= QueryTriggerInteraction.UseGlobal &&
                value <= QueryTriggerInteraction.Collide;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
