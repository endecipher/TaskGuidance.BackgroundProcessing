using ActivityLogger.Logging;
using System.Threading;

namespace TaskGuidance.BackgroundProcessing.Cancellation
{
    public class CancellationManager : ICancellationManager
    {
        #region Constants

        public const string CancellationTriggered = nameof(CancellationTriggered);
        public const string CancellationTriggeredViaBinding = nameof(CancellationTriggeredViaBinding);
        public const string BindingTriggered = nameof(BindingTriggered);
        public const string Token = nameof(Token);
        public const string CancellationManagerEntity = nameof(CancellationManager);
        public const string TimeOut = nameof(TimeOut);

        #endregion

        public CancellationManager(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
            Refresh();
        }

        private CancellationTokenSource InternalSource { get; set; }

        public CancellationToken CoreToken => InternalSource.Token;

        IActivityLogger ActivityLogger { get; }

        public void TriggerCancellation()
        {
            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = CancellationManagerEntity,
                Event = CancellationTriggered,
                Level = ActivityLogLevel.Debug,

            }
            .With(ActivityParam.New(Token, InternalSource.Token.ToString()))
            .WithCallerInfo());

            if (InternalSource.Token.CanBeCanceled)
                InternalSource.Cancel(true);
            else
                InternalSource.Token.ThrowIfCancellationRequested();
        }

        public void Bind(CancellationToken token)
        {
            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = CancellationManagerEntity,
                Event = BindingTriggered,
                Level = ActivityLogLevel.Verbose,

            }
            .With(ActivityParam.New(Token, token.ToString()))
            .WithCallerInfo());

            token.Register(() =>
            {
                ActivityLogger?.Log(new Logging.GuidanceActivity
                {
                    EntitySubject = CancellationManagerEntity,
                    Event = CancellationTriggeredViaBinding,
                    Level = ActivityLogLevel.Debug,

                }
                .With(ActivityParam.New(Token, token.ToString()))
                .WithCallerInfo());

                InternalSource.Cancel();
            });
        }

        public void Refresh()
        {
            InternalSource?.Dispose();
            InternalSource = new CancellationTokenSource();
        }

        public void ThrowIfCancellationRequested()
        {
            if (CoreToken.IsCancellationRequested)
            {
                CoreToken.ThrowIfCancellationRequested();
            }
        }
    }
}
