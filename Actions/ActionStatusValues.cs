namespace TaskGuidance.BackgroundProcessing.Actions
{
    public enum ActionStatusValues
    {
        New = 0,
        Processing = 1,
        Completed = 2,
        Faulted = 3,
        Cancelled = 4,
        TimedOut = 5,
        Stopped = 6,
        Skipped = 7
    }
}
