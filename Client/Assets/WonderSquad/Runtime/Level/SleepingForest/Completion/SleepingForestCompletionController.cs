using System;
using UnityEngine;
using WonderSquad.Core.Logging;
using WonderSquad.Puzzle.SleepingForest;

namespace WonderSquad.SleepingForest.Completion
{
    /// <summary>
    /// Owns the irreversible scene-local completion result derived from the
    /// trial snapshot and slice-end player presence.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SleepingForestCompletionController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Read-only Forest Signal Trial source in this scene.")]
        private ForestSignalTrial forestSignalTrial;

        [SerializeField]
        [Tooltip("Scene-local endpoint presence source.")]
        private SleepingForestSliceEndTrigger sliceEndTrigger;

        private SleepingForestCompletionSnapshot currentSnapshot;
        private SleepingForestCompletionFeedbackState feedbackState;
        private uint lastObservedTrialRevision;
        private bool isTrialCompleted;
        private bool isPlayerAtEnd;
        private bool hasObservedTrialSnapshot;
        private bool hasInitialized;
        private bool isSubscribed;

        public event Action<SleepingForestCompletionSnapshot>
            CompletionChanged;

        public event Action<SleepingForestCompletionSnapshot> Completed;

        public event Action<SleepingForestCompletionFeedbackState>
            FeedbackStateChanged;

        public ForestSignalTrial ForestSignalTrial => forestSignalTrial;

        public SleepingForestSliceEndTrigger SliceEndTrigger =>
            sliceEndTrigger;

        public SleepingForestCompletionSnapshot CurrentSnapshot
        {
            get
            {
                EnsureInitialized();
                return currentSnapshot;
            }
        }

        public SleepingForestCompletionFeedbackState FeedbackState
        {
            get
            {
                EnsureInitialized();
                return feedbackState;
            }
        }

        public bool IsPlayerAtEnd => isPlayerAtEnd;

        public uint LastObservedTrialRevision =>
            lastObservedTrialRevision;

        public bool HasValidConfiguration =>
            forestSignalTrial != null &&
            sliceEndTrigger != null &&
            sliceEndTrigger.HasValidConfiguration &&
            forestSignalTrial.gameObject.scene == gameObject.scene &&
            sliceEndTrigger.gameObject.scene == gameObject.scene;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Supplies explicit scene composition references before enable.
        /// </summary>
        public bool Configure(
            ForestSignalTrial trial,
            SleepingForestSliceEndTrigger endTrigger)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "SleepingForestCompletionController cannot be " +
                    "reconfigured while enabled.");
                return false;
            }

            Unsubscribe();
            forestSignalTrial = trial;
            sliceEndTrigger = endTrigger;
            hasInitialized = false;
            hasObservedTrialSnapshot = false;
            lastObservedTrialRevision = 0U;
            isTrialCompleted = false;
            isPlayerAtEnd = false;
            EnsureInitialized();
            return HasValidConfiguration;
        }

        private void EnsureInitialized()
        {
            if (hasInitialized)
            {
                return;
            }

            currentSnapshot = new SleepingForestCompletionSnapshot(
                SleepingForestCompletionState.NotCompleted,
                0U,
                0U);
            feedbackState =
                SleepingForestCompletionFeedbackState.Hidden;
            hasInitialized = true;
        }

        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            if (!HasValidConfiguration)
            {
                ProjectLog.Error(
                    "SleepingForestCompletionController requires one " +
                    "scene-local Trial and Slice End Trigger.");
                return;
            }

            forestSignalTrial.TrialStateChanged +=
                HandleTrialStateChanged;
            sliceEndTrigger.PlayerPresenceChanged +=
                HandlePlayerPresenceChanged;
            isSubscribed = true;

            ApplyTrialSnapshot(forestSignalTrial.CurrentSnapshot);
            ApplyPlayerPresence(sliceEndTrigger.IsPlayerPresent);
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (forestSignalTrial != null)
            {
                forestSignalTrial.TrialStateChanged -=
                    HandleTrialStateChanged;
            }

            if (sliceEndTrigger != null)
            {
                sliceEndTrigger.PlayerPresenceChanged -=
                    HandlePlayerPresenceChanged;
            }

            isSubscribed = false;
        }

        private void HandleTrialStateChanged(
            ForestSignalTrialSnapshot snapshot)
        {
            ApplyTrialSnapshot(snapshot);
        }

        private void HandlePlayerPresenceChanged(bool isPresent)
        {
            ApplyPlayerPresence(isPresent);
        }

        private void ApplyTrialSnapshot(
            ForestSignalTrialSnapshot snapshot)
        {
            if (hasObservedTrialSnapshot &&
                snapshot.Revision < lastObservedTrialRevision)
            {
                return;
            }

            if (hasObservedTrialSnapshot &&
                snapshot.Revision == lastObservedTrialRevision &&
                snapshot.IsCompleted == isTrialCompleted)
            {
                return;
            }

            hasObservedTrialSnapshot = true;
            lastObservedTrialRevision = snapshot.Revision;
            isTrialCompleted = snapshot.IsCompleted;
            EvaluateCompletion();
        }

        private void ApplyPlayerPresence(bool isPresent)
        {
            if (isPlayerAtEnd == isPresent)
            {
                EvaluateCompletion();
                return;
            }

            isPlayerAtEnd = isPresent;
            EvaluateCompletion();
        }

        private void EvaluateCompletion()
        {
            EnsureInitialized();
            var wasCompleted = currentSnapshot.IsCompleted;
            if (!wasCompleted && isTrialCompleted && isPlayerAtEnd)
            {
                currentSnapshot = new SleepingForestCompletionSnapshot(
                    SleepingForestCompletionState.Completed,
                    currentSnapshot.CompletionRevision + 1U,
                    lastObservedTrialRevision);
                CompletionChanged?.Invoke(currentSnapshot);
                Completed?.Invoke(currentSnapshot);
            }

            var nextFeedbackState = ResolveFeedbackState();
            if (feedbackState == nextFeedbackState)
            {
                return;
            }

            feedbackState = nextFeedbackState;
            FeedbackStateChanged?.Invoke(feedbackState);
        }

        private SleepingForestCompletionFeedbackState
            ResolveFeedbackState()
        {
            if (currentSnapshot.IsCompleted)
            {
                return SleepingForestCompletionFeedbackState.Completed;
            }

            return isPlayerAtEnd && !isTrialCompleted
                ? SleepingForestCompletionFeedbackState.ConditionsUnmet
                : SleepingForestCompletionFeedbackState.Hidden;
        }
    }
}
