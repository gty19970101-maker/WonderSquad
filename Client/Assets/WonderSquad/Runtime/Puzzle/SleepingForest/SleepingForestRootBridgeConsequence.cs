using UnityEngine;

namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Consumes one explicit ForestSignalTrial instance as read-only input and
    /// maps its snapshot to the local Root Bridge shortcut consequence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SleepingForestRootBridgeConsequence : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Scene-local trial that exposes immutable snapshots and an instance event.")]
        private ForestSignalTrial trial;

        [SerializeField]
        [Tooltip("Independent shortcut surface. The main route is intentionally not referenced.")]
        private RootBridgeAdvantageSpan advantageSpan;

        [SerializeField]
        [Tooltip("Instance-local visual feedback for the shortcut.")]
        private RootBridgeAdvantageVisual advantageVisual;

        private SleepingForestRootBridgeState currentState;
        private uint lastAppliedRevision;
        private bool hasAppliedSnapshot;

        public ForestSignalTrial Trial => trial;

        public RootBridgeAdvantageSpan AdvantageSpan => advantageSpan;

        public RootBridgeAdvantageVisual AdvantageVisual => advantageVisual;

        public SleepingForestRootBridgeState CurrentState => currentState;

        public uint LastAppliedRevision => lastAppliedRevision;

        public bool HasValidConfiguration =>
            trial != null &&
            advantageSpan != null &&
            advantageVisual != null &&
            trial.gameObject.scene == gameObject.scene &&
            advantageSpan.gameObject.scene == gameObject.scene &&
            advantageVisual.gameObject.scene == gameObject.scene &&
            advantageSpan.HasValidConfiguration &&
            advantageVisual.HasValidConfiguration;

        private void Awake()
        {
            InitializeInitialState();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            hasAppliedSnapshot = false;
        }

        /// <summary>
        /// Supplies explicit scene composition references before gameplay
        /// begins. This does not configure or mutate the trial.
        /// </summary>
        public bool Configure(
            ForestSignalTrial owningTrial,
            RootBridgeAdvantageSpan shortcutSpan,
            RootBridgeAdvantageVisual shortcutVisual)
        {
            Unsubscribe();
            trial = owningTrial;
            advantageSpan = shortcutSpan;
            advantageVisual = shortcutVisual;
            hasAppliedSnapshot = false;
            currentState = SleepingForestRootBridgeState.Initial;
            lastAppliedRevision = 0U;
            InitializeInitialState();
            if (isActiveAndEnabled)
            {
                Subscribe();
            }

            return HasValidConfiguration;
        }

        private void Subscribe()
        {
            if (!HasValidConfiguration)
            {
                return;
            }

            trial.TrialStateChanged -= OnTrialStateChanged;
            trial.TrialStateChanged += OnTrialStateChanged;
            ApplySnapshot(trial.CurrentSnapshot);
        }

        private void Unsubscribe()
        {
            if (trial != null)
            {
                trial.TrialStateChanged -= OnTrialStateChanged;
            }
        }

        private void OnTrialStateChanged(ForestSignalTrialSnapshot snapshot)
        {
            ApplySnapshot(snapshot);
        }

        private void InitializeInitialState()
        {
            if (advantageSpan == null || advantageVisual == null)
            {
                return;
            }

            advantageSpan.InitializeState();
            advantageVisual.InitializeState();
            currentState = SleepingForestRootBridgeState.Initial;
        }

        private void ApplySnapshot(ForestSignalTrialSnapshot snapshot)
        {
            if (!HasValidConfiguration ||
                (hasAppliedSnapshot && snapshot.Revision <= lastAppliedRevision))
            {
                return;
            }

            var state = snapshot.IsCompleted
                ? SleepingForestRootBridgeState.Activated
                : SleepingForestRootBridgeState.Initial;
            advantageSpan.ApplyState(state);
            advantageVisual.ApplyState(state);
            currentState = state;
            lastAppliedRevision = snapshot.Revision;
            hasAppliedSnapshot = true;
        }
    }
}
