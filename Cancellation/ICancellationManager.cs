using System.Threading;

namespace TaskGuidance.BackgroundProcessing.Cancellation
{
    /// <summary>
    /// Assists in cancellation activities
    /// </summary>
    public interface ICancellationManager
    {
        void Refresh();
        void Bind(CancellationToken token);
        void TriggerCancellation();
        void ThrowIfCancellationRequested();
        CancellationToken CoreToken { get; }
    }
}
