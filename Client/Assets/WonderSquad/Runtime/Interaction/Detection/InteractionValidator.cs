using UnityEngine;
using WonderSquad.Core.Contracts.Interaction;

namespace WonderSquad.Interaction.Detection
{
    /// <summary>
    /// Applies side-effect-free validity rules to a detected candidate.
    /// Physics evidence is collected by InteractionDetector.
    /// </summary>
    public sealed class InteractionValidator
    {
        public bool TryValidate(
            IInteractable candidate,
            int candidateLayer,
            Vector3 originPosition,
            InteractionSettings settings,
            bool isOccluded,
            out float squaredDistance)
        {
            squaredDistance = 0f;
            if (!IsAlive(candidate) ||
                settings == null ||
                !settings.IsValid ||
                !candidate.TargetId.IsValid ||
                !candidate.IsDetectionEnabled ||
                !IsLayerIncluded(
                    settings.InteractionLayerMask,
                    candidateLayer) ||
                isOccluded ||
                !IsFinite(originPosition) ||
                !IsFinite(candidate.DetectionPosition))
            {
                return false;
            }

            squaredDistance =
                (candidate.DetectionPosition - originPosition).sqrMagnitude;
            var maximumSquaredDistance =
                settings.DetectionRadius * settings.DetectionRadius;
            return squaredDistance <= maximumSquaredDistance;
        }

        private static bool IsLayerIncluded(
            LayerMask layerMask,
            int layer)
        {
            if (layer < 0 || layer > 31)
            {
                return false;
            }

            return (layerMask.value & (1 << layer)) != 0;
        }

        private static bool IsAlive(IInteractable candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            return
                !(candidate is UnityEngine.Object unityObject) ||
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
