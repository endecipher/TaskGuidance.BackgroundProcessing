using System;
using System.Threading;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    /// <summary>
    /// WaitHandle for obtaining outputs from blocking <see cref="IAction"/>.
    /// </summary>
    public interface IActionLock : IDisposable
    {
        void SignalDone();
        void Wait();
        void Wait(TimeSpan timeOut, CancellationToken cancellationToken);
    }
}