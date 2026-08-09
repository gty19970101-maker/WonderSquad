using UnityEngine;
using WonderSquad.Core.Logging;
using WonderSquad.SleepingForest.Completion;

namespace WonderSquad.UI.SleepingForest
{
    /// <summary>
    /// Maps read-only scene completion state to screen and world views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SleepingForestCompletionPresenter : MonoBehaviour
    {
        public const string RequirementMessageKey =
            "sleeping_forest.completion.requirement_incomplete";
        public const string RequirementFallbackMessage =
            "Forest signal incomplete";
        public const string CompletionMessageKey =
            "sleeping_forest.completion.complete";
        public const string CompletionFallbackMessage =
            "Sleeping Forest Complete";

        [SerializeField]
        [Tooltip("Scene-local source of immutable completion state.")]
        private SleepingForestCompletionController completionController;

        [SerializeField]
        [Tooltip("Display-only screen feedback view.")]
        private SleepingForestCompletionView completionView;

        [SerializeField]
        [Tooltip("Display-only world marker view.")]
        private SleepingForestCompletionMarkerView markerView;

        private bool isSubscribed;

        public SleepingForestCompletionController CompletionController =>
            completionController;

        public SleepingForestCompletionView CompletionView =>
            completionView;

        public SleepingForestCompletionMarkerView MarkerView => markerView;

        public bool HasValidConfiguration =>
            completionController != null &&
            completionView != null &&
            markerView != null &&
            completionView.HasValidConfiguration &&
            markerView.HasValidConfiguration;

        private void OnEnable()
        {
            if (!HasValidConfiguration)
            {
                ProjectLog.Error(
                    "SleepingForestCompletionPresenter requires an " +
                    "explicit Controller, View and MarkerView.");
                enabled = false;
                return;
            }

            Subscribe();
            RenderCurrentState();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (completionView != null)
            {
                completionView.ResetPresentation();
            }

            if (markerView != null)
            {
                markerView.ResetPresentation();
            }
        }

        public bool Configure(
            SleepingForestCompletionController controller,
            SleepingForestCompletionView screenView,
            SleepingForestCompletionMarkerView worldMarkerView)
        {
            if (isActiveAndEnabled)
            {
                ProjectLog.Error(
                    "SleepingForestCompletionPresenter cannot be " +
                    "reconfigured while enabled.");
                return false;
            }

            completionController = controller;
            completionView = screenView;
            markerView = worldMarkerView;
            return HasValidConfiguration;
        }

        private void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            completionController.CompletionChanged +=
                HandleCompletionChanged;
            completionController.FeedbackStateChanged +=
                HandleFeedbackStateChanged;
            completionView.BecameAvailable += HandleViewBecameAvailable;
            markerView.BecameAvailable += HandleViewBecameAvailable;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            if (completionController != null)
            {
                completionController.CompletionChanged -=
                    HandleCompletionChanged;
                completionController.FeedbackStateChanged -=
                    HandleFeedbackStateChanged;
            }

            if (completionView != null)
            {
                completionView.BecameAvailable -=
                    HandleViewBecameAvailable;
            }

            if (markerView != null)
            {
                markerView.BecameAvailable -=
                    HandleViewBecameAvailable;
            }

            isSubscribed = false;
        }

        private void HandleCompletionChanged(
            SleepingForestCompletionSnapshot snapshot)
        {
            RenderCurrentState();
        }

        private void HandleFeedbackStateChanged(
            SleepingForestCompletionFeedbackState state)
        {
            RenderCurrentState();
        }

        private void HandleViewBecameAvailable()
        {
            RenderCurrentState();
        }

        private void RenderCurrentState()
        {
            if (!HasValidConfiguration)
            {
                return;
            }

            var feedbackState = completionController.FeedbackState;
            completionView.Render(CreateViewData(feedbackState));
            markerView.Render(feedbackState);
        }

        private static SleepingForestCompletionViewData CreateViewData(
            SleepingForestCompletionFeedbackState feedbackState)
        {
            switch (feedbackState)
            {
                case SleepingForestCompletionFeedbackState.ConditionsUnmet:
                    return new SleepingForestCompletionViewData(
                        feedbackState,
                        RequirementMessageKey,
                        RequirementFallbackMessage,
                        true);

                case SleepingForestCompletionFeedbackState.Completed:
                    return new SleepingForestCompletionViewData(
                        feedbackState,
                        CompletionMessageKey,
                        CompletionFallbackMessage,
                        true);

                default:
                    return new SleepingForestCompletionViewData(
                        SleepingForestCompletionFeedbackState.Hidden,
                        string.Empty,
                        string.Empty,
                        false);
            }
        }
    }
}
