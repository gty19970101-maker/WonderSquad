namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Represents progress through the fixed A-to-B forest signal trial.
    /// </summary>
    public enum ForestSignalTrialPhase
    {
        Unknown = 0,
        AwaitingFirstBeacon = 1,
        AwaitingSecondBeacon = 2,
        Completed = 3
    }
}
