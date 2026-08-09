using System;
using UnityEngine;
using WonderSquad.SleepingForest.Completion;

namespace WonderSquad.UI.SleepingForest
{
    /// <summary>
    /// Controls the non-colliding world marker for completed slice feedback.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SleepingForestCompletionMarkerView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Non-colliding marker revealed after completion.")]
        private GameObject completedMarker;

        private SleepingForestCompletionFeedbackState displayedState;
        private bool hasDisplayedState;
        private int refreshCount;

        public event Action BecameAvailable;

        public GameObject CompletedMarker => completedMarker;

        public bool IsCompletedMarkerVisible =>
            completedMarker != null && completedMarker.activeSelf;

        public int RefreshCount => refreshCount;

        public bool HasValidConfiguration => completedMarker != null;

        private void Awake()
        {
            ResetPresentation();
        }

        private void OnEnable()
        {
            ResetPresentation();
            BecameAvailable?.Invoke();
        }

        private void OnDisable()
        {
            ResetPresentation();
        }

        public bool Configure(GameObject localCompletedMarker)
        {
            completedMarker = localCompletedMarker;
            ResetPresentation();
            return HasValidConfiguration;
        }

        public bool Render(
            SleepingForestCompletionFeedbackState feedbackState)
        {
            if (hasDisplayedState && displayedState == feedbackState)
            {
                return false;
            }

            displayedState = feedbackState;
            hasDisplayedState = true;
            refreshCount++;
            completedMarker.SetActive(
                feedbackState ==
                SleepingForestCompletionFeedbackState.Completed);
            return true;
        }

        public void ResetPresentation()
        {
            displayedState =
                SleepingForestCompletionFeedbackState.Hidden;
            hasDisplayedState = false;
            if (completedMarker != null)
            {
                completedMarker.SetActive(false);
            }
        }
    }
}
