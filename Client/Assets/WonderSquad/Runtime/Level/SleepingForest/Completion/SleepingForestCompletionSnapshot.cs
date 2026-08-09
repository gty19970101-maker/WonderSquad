using System;

namespace WonderSquad.SleepingForest.Completion
{
    /// <summary>
    /// Exposes immutable scene completion data without Unity object references.
    /// </summary>
    public readonly struct SleepingForestCompletionSnapshot :
        IEquatable<SleepingForestCompletionSnapshot>
    {
        public SleepingForestCompletionSnapshot(
            SleepingForestCompletionState state,
            uint completionRevision,
            uint trialRevisionAtCompletion)
        {
            State = state;
            CompletionRevision = completionRevision;
            TrialRevisionAtCompletion = trialRevisionAtCompletion;
        }

        public SleepingForestCompletionState State { get; }

        public bool IsCompleted =>
            State == SleepingForestCompletionState.Completed;

        public uint CompletionRevision { get; }

        public uint TrialRevisionAtCompletion { get; }

        public bool Equals(SleepingForestCompletionSnapshot other)
        {
            return
                State == other.State &&
                CompletionRevision == other.CompletionRevision &&
                TrialRevisionAtCompletion == other.TrialRevisionAtCompletion;
        }

        public override bool Equals(object obj)
        {
            return
                obj is SleepingForestCompletionSnapshot other &&
                Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)State;
                hashCode = (hashCode * 397) ^ (int)CompletionRevision;
                hashCode =
                    (hashCode * 397) ^
                    (int)TrialRevisionAtCompletion;
                return hashCode;
            }
        }

        public static bool operator ==(
            SleepingForestCompletionSnapshot left,
            SleepingForestCompletionSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            SleepingForestCompletionSnapshot left,
            SleepingForestCompletionSnapshot right)
        {
            return !left.Equals(right);
        }
    }
}
