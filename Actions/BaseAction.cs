using ActivityLogger.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskGuidance.BackgroundProcessing.Cancellation;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    /// <summary>
    /// Base Wrapper Action for background processing
    /// </summary>
    /// <typeparam name="TInput">Input Type</typeparam>
    /// <typeparam name="TOutput">Output Type. <see cref="object"/> may be specified for non blocking actions</typeparam>
    public abstract class BaseAction<TInput, TOutput> : IAction, IJettonExecutor where TInput : class where TOutput : class
    {
        #region Constants

        private const string ActionEntity = nameof(IAction);
        private const string Exception = nameof(Exception);
        private const string _New = nameof(_New);
        private const string UniqueActionName = nameof(UniqueActionName);
        private const string _Begin = nameof(_Begin);
        private const string _Proceeding = nameof(_Proceeding);
        private const string _CoreActionStarting = nameof(_CoreActionStarting);
        private const string _CoreActionEnded = nameof(_CoreActionEnded);
        private const string _SkipProceeding = nameof(_SkipProceeding);
        private const string _OnTimeOut = nameof(_OnTimeOut);
        private const string _OnCancellation = nameof(_OnCancellation);
        private const string _OnFailure = nameof(_OnFailure);
        private const string _OnException = nameof(_OnException);
        private const string _PostProcessSignalling = nameof(_PostProcessSignalling);

        #endregion

        #region Dependencies 
        /// <summary>
        /// Transient Cancellation Manager associated with this Action
        /// </summary>
        public ICancellationManager CancellationManager { get; protected set; }
        protected IActivityLogger ActivityLogger { get; set; }

        #endregion

        protected TInput Input = null;
        public abstract TimeSpan TimeOut { get; }
        public virtual ActionPriorityValues PriorityValue { get; set; } = ActionPriorityValues.Medium;
        public abstract string UniqueName { get; }

        public BaseAction(TInput input, IActivityLogger activityLogger = null)
        {
            Input = input;
            ActivityLogger = activityLogger;

            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ActionEntity,
                Event = _New,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(UniqueActionName, UniqueName))
            .WithCallerInfo());
        }

        public void SupportCancellation(ICancellationManager cancellationManager = null)
        {
            CancellationManager = cancellationManager ?? new CancellationManager(ActivityLogger);
        }

        #region Workflow Actions

        /// <summary>
        /// Returns Default Output of the Action. Used to initialize the Output.
        /// In any event of failure, the defult Output would be considered for Blocking <see cref="BaseAction{TInput, TOutput}"/>.
        /// </summary>
        protected virtual TOutput DefaultOutput()
        {
            return default;
        }

        /// <summary>
        /// Precondition to check whether the <see cref="Action(CancellationToken)"/> should continue.
        /// </summary>
        protected abstract Task<bool> ShouldProceed();

        /// <summary>
        /// Core Action to perform. Called within a TimeOut wrapper using <see cref="TimeOut"/>
        /// </summary>
        /// <param name="cancellationToken">Implicit usage. If <see cref="SupportCancellation(ICancellationManager)"/> is called, <see cref="ICancellationManager.CoreToken"/> is used. Else <see cref="CancellationToken.None"/> is used.</param>
        protected abstract Task<TOutput> Action(CancellationToken cancellationToken);

        /// <summary>
        /// Fired when the Core Action successfully completes.
        /// </summary>
        /// <param name="outputObtained">Output obtained from the previously executed <see cref="Action(CancellationToken)"/></param>
        protected virtual async Task PostAction(TOutput outputObtained)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired when Timeout Exception is caught.
        /// </summary>
        protected virtual async Task OnTimeOut()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired when the Action is cancelled externally and TaskCanceledException/OperationCanceledException is caught.
        /// </summary>
        protected virtual async Task OnCancellation()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired when a fault/unknown error occurs
        /// </summary>
        protected virtual async Task OnFailure()
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Fired after the <see cref="IActionJetton.SetResultIfAny{T}(T, System.Exception)"/> is called. 
        /// Custom logic after releasing the waiting caller thread may be defined here.
        /// </summary>
        protected virtual async Task OnActionEnd()
        {
            await Task.CompletedTask;
        }

        #endregion

        /// <summary>
        /// Action Workflow - to be started from the <see cref="Core.ITaskProcessingEngine"/> in the background
        /// </summary>
        async Task IJettonExecutor.Perform(IActionJetton jetton)
        {
            TOutput output = DefaultOutput();
            Exception exception = null;

            try
            {
                Log(_Begin);

                if (await ShouldProceed())
                {
                    Log(_Proceeding);

                    CancellationManager?.ThrowIfCancellationRequested();

                    jetton.MoveToProcessing();

                    Log(_CoreActionStarting);

                    output = await Action(CancellationManager?.CoreToken ?? CancellationToken.None).WithTimeOut(TimeOut, CancellationManager?.CoreToken);

                    Log(_CoreActionEnded);

                    await PostAction(output);

                    jetton.MoveToCompleted();
                }
                else
                {
                    Log(_SkipProceeding);

                    jetton.MoveToSkipped();
                }
            }
            catch (TimeoutException timeOutException)
            {
                exception = timeOutException;

                jetton.MoveToTimeOut();

                Log(_OnTimeOut, timeOutException);

                await OnTimeOut();
            }
            catch (AggregateException aggregateException)
            {
                exception = aggregateException;

                var ex = aggregateException.InnerExceptions.First();

                foreach (var exceptionItem in aggregateException.InnerExceptions)
                {
                    Log(_OnException, exceptionItem);
                }

                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    jetton.MoveToCancelled();

                    Log(_OnCancellation, ex);

                    await OnCancellation();
                }
                else
                {
                    jetton.MoveToFaulted();

                    Log(_OnFailure, ex);

                    await OnFailure();
                }
            }
            catch (Exception e)
            {
                exception = e;

                jetton.MoveToFaulted();

                Log(_OnFailure, e);

                await OnFailure();
            }

            Log(_PostProcessSignalling);

            jetton.SetResultIfAny(output, exception);

            await OnActionEnd();
        }

        private void Log(string @event, Exception ex = null)
        {
            bool hasException = ex != null;

            Activity activity = new Logging.GuidanceActivity
            {
                EntitySubject = UniqueName,
                Event = @event,
                Level = !hasException ? ActivityLogLevel.Verbose : ActivityLogLevel.Error,
            };

            if (hasException)
            {
                activity = activity.With(ActivityParam.New(Exception, ex));
            }

            ActivityLogger?.Log(activity.WithCallerInfo());
        }

        IActionJetton IJettonExecutor.ReturnJetton()
        {
            return new ActionJetton(action: this).WithLogger(ActivityLogger);
        }
    }
}
