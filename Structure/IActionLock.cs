using System;
using System.Threading;

namespace EventGuidance.Structure
{
    public interface IActionLock : IDisposable
    {
        void SignalDone();
        void Wait();
        void Wait(TimeSpan timeOut, CancellationToken cancellationToken);
    }

}