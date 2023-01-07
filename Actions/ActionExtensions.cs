using System;
using System.Threading;
using System.Threading.Tasks;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    public static class ActionExtensions
    {
        public static async Task<TResult> WithTimeOut<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken? cancellationToken)
        {
            if (task == await Task.WhenAny(task, Task.Delay(timeout, cancellationToken ?? CancellationToken.None)))
            {
                return await task;
            }

            throw new TimeoutException();
        }
    }
}
