namespace WonderSquad.Puzzle.SleepingForest
{
    /// <summary>
    /// Represents the authoritative local state of one forest beacon.
    /// Activated is terminal for Sprint003B.
    /// </summary>
    public enum ForestBeaconLogicalState
    {
        Unknown = 0,
        Dormant = 1,
        Activated = 2
    }
}
