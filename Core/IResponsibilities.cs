using System.Collections.Generic;
using System.Threading;
using TaskGuidance.BackgroundProcessing.Actions;

namespace TaskGuidance.BackgroundProcessing.Core
{
    /// <summary>
    /// Entry-point for enqueuing <see cref="BaseAction{TInput, TOutput}"/> and controls
    /// </summary>
    public interface IResponsibilities
    {
        /// <summary>
        /// Caller supplied Unique Identifier
        /// </summary>
        string UniqueIdentifier { get; }

        /// <summary>
        /// Initialize a fresh set of Responsibilities
        /// </summary>
        /// <param name="invocableActionNames">If supplied, validates <see cref="BaseAction{TInput, TOutput}.UniqueName"/> to be present during any enqueue operations</param>
        /// <param name="identifier">Placeholder Identifier which gets assigned to <see cref="UniqueIdentifier"/></param>
        void ConfigureNew(HashSet<string> invocableActionNames = null, string identifier = null);

        /// <summary>
        /// Enqueues a blocking Action (which returns an output). Caller thread must wait until Action is processed offline and output is obtained.
        /// </summary>
        /// <typeparam name="TOutput">Output type of the <see cref="BaseAction{TInput, TOutput}"/></typeparam>
        /// <param name="action">Action/Logic to perform</param>
        /// <param name="executeSeparately">If supplied <c>true</c>, global cancellation will not affect the <paramref name="action"/></param>
        /// <returns></returns>
        TOutput QueueBlockingAction<TOutput>(IAction action, bool executeSeparately = false) where TOutput : class;

        /// <summary>
        /// Enqueues a non-blocking Action (which do not need an output to be awaited). Caller thread does not wait until Action is processed offline.
        /// </summary>
        /// <param name="action">Action/Logic to perform</param>
        /// <param name="executeSeparately">If supplied <c>true</c>, global cancellation will not affect the <paramref name="action"/></param>
        /// <returns></returns>
        void QueueAction(IAction action, bool executeSeparately = false);

        /// <summary>
        /// Global Cancellation Token for use.
        /// </summary>
        CancellationToken GlobalCancellationToken { get; }

        /// <summary>
        /// Cancels all responsibilities and actions which are ongoing and were not enqueued separately.
        /// </summary>
        void TriggerGlobalCancellation();
    }
}
