using System.Threading;
using TaskGuidance.BackgroundProcessing.Actions;

namespace TaskGuidance.BackgroundProcessing.Core
{
    /// <summary>
    /// Hoists the Global Background Processing and uses the Concurrent Priority Queue
    /// </summary>
    public interface ITaskProcessingEngine
    {
        void StartProcessing(CancellationToken token);
        void Stop();
        void Enqueue(IActionJetton info);
    }
}
