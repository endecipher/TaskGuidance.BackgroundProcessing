using System;
using TaskGuidance.BackgroundProcessing.Cancellation;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    public interface IAction
    {
        ICancellationManager CancellationManager { get; }

        ActionPriorityValues PriorityValue { get; }

        TimeSpan TimeOut { get; }

        string UniqueName { get; }
    }
}
