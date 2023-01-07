using System.Threading.Tasks;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    /// <summary>
    /// Implemented by <see cref="BaseAction{TInput, TOutput}"/> for obtaining a <see cref="Task"/> which wraps the complete action workflow.
    /// </summary>
    public interface IJettonExecutor
    {
        Task Perform(IActionJetton jetton);

        IActionJetton ReturnJetton();
    }
}
