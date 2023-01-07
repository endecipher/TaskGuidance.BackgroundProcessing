using System.Threading;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    public class ActionLock : IActionLock
    {
        public ActionLock(bool canReset = false)
        {
            CanReset = canReset;
        }

        ManualResetEventSlim Lock = new ManualResetEventSlim(false);

        public bool CanReset { get; }

        public void Dispose()
        {
            Lock.Dispose();
            Lock = null;
        }

        public void SignalDone()
        {
            Lock?.Set();

            if (CanReset)
                Lock?.Reset();
        }

        public void Wait(System.TimeSpan timeOut, CancellationToken cancellationToken)
        {
            Lock?.Wait(timeOut, cancellationToken);
        }

        public void Wait()
        {
            Lock?.Wait();
        }
    }
}
