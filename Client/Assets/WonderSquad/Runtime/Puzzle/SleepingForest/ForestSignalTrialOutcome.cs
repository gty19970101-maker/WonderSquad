namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Describes the latest accepted domain result independently from the
    /// generic interaction result code.
    /// </summary>
    public enum ForestSignalTrialOutcome
    {
        None = 0,
        FirstBeaconActivated = 1,
        IncorrectOrder = 2,
        TrialCompleted = 3
    }
}
