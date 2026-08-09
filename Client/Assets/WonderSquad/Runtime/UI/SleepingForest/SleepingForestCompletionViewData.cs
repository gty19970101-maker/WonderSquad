using System;
using WonderSquad.SleepingForest.Completion;

namespace WonderSquad.UI.SleepingForest
{
    /// <summary>
    /// Immutable presentation data for Sleeping Forest completion feedback.
    /// </summary>
    public readonly struct SleepingForestCompletionViewData :
        IEquatable<SleepingForestCompletionViewData>
    {
        public SleepingForestCompletionViewData(
            SleepingForestCompletionFeedbackState feedbackState,
            string messageKey,
            string fallbackMessage,
            bool isVisible)
        {
            FeedbackState = feedbackState;
            MessageKey = messageKey ?? string.Empty;
            FallbackMessage = fallbackMessage ?? string.Empty;
            IsVisible = isVisible;
        }

        public SleepingForestCompletionFeedbackState FeedbackState
        {
            get;
        }

        public string MessageKey { get; }

        public string FallbackMessage { get; }

        public bool IsVisible { get; }

        public bool Equals(SleepingForestCompletionViewData other)
        {
            return
                FeedbackState == other.FeedbackState &&
                MessageKey == other.MessageKey &&
                FallbackMessage == other.FallbackMessage &&
                IsVisible == other.IsVisible;
        }

        public override bool Equals(object obj)
        {
            return
                obj is SleepingForestCompletionViewData other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)FeedbackState;
                hashCode =
                    (hashCode * 397) ^
                    StringComparer.Ordinal.GetHashCode(MessageKey);
                hashCode =
                    (hashCode * 397) ^
                    StringComparer.Ordinal.GetHashCode(FallbackMessage);
                hashCode = (hashCode * 397) ^ IsVisible.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(
            SleepingForestCompletionViewData left,
            SleepingForestCompletionViewData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            SleepingForestCompletionViewData left,
            SleepingForestCompletionViewData right)
        {
            return !left.Equals(right);
        }
    }
}
