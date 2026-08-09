using UnityEngine;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Owns the independently authored shortcut surface. It never decides
    /// trial state and never changes the existing main-route crossing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RootBridgeAdvantageSpan : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Renderer for the single shortcut ground surface.")]
        private Renderer groundRenderer;

        [SerializeField]
        [Tooltip("Collider that supports the player only while the shortcut is active.")]
        private Collider groundCollider;

        private SleepingForestRootBridgeState currentState;
        private bool hasAppliedState;

        public SleepingForestRootBridgeState CurrentState => currentState;

        public Renderer GroundRenderer => groundRenderer;

        public Collider GroundCollider => groundCollider;

        public bool HasValidConfiguration =>
            groundRenderer != null &&
            groundCollider != null &&
            groundRenderer.gameObject == groundCollider.gameObject;

        /// <summary>
        /// Supplies explicit scene composition references before the object
        /// starts participating in gameplay.
        /// </summary>
        public bool Configure(
            Renderer shortcutGroundRenderer,
            Collider shortcutGroundCollider)
        {
            groundRenderer = shortcutGroundRenderer;
            groundCollider = shortcutGroundCollider;
            return HasValidConfiguration;
        }

        /// <summary>
        /// Applies the already-decided local environment state. Reapplying
        /// the same state is intentionally a no-op.
        /// </summary>
        public bool ApplyState(SleepingForestRootBridgeState state)
        {
            if (!HasValidConfiguration)
            {
                return false;
            }

            var shouldActivate =
                state == SleepingForestRootBridgeState.Activated;
            var hasChanged =
                !hasAppliedState ||
                currentState != state ||
                groundRenderer.enabled != shouldActivate ||
                groundCollider.enabled != shouldActivate;
            if (groundRenderer.enabled != shouldActivate)
            {
                groundRenderer.enabled = shouldActivate;
            }

            if (groundCollider.enabled != shouldActivate)
            {
                groundCollider.enabled = shouldActivate;
            }

            currentState = state;
            hasAppliedState = true;
            return hasChanged;
        }

        /// <summary>
        /// Forces the authored initial state once, including after a scene
        /// reload, without touching any existing main-route geometry.
        /// </summary>
        public void InitializeState()
        {
            hasAppliedState = false;
            ApplyState(SleepingForestRootBridgeState.Initial);
        }
    }
}
