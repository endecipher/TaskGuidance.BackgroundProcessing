using ActivityLogger.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaskGuidance.BackgroundProcessing.Actions;
using TaskGuidance.BackgroundProcessing.Cancellation;

namespace TaskGuidance.BackgroundProcessing.Core
{
    public class Responsibilities : IResponsibilities
    {
        #region Constants

        public const string ResponsibilitiesEntity = nameof(Responsibilities);
        public const string ConfiguringNew = nameof(ConfiguringNew);
        public const string QueuingEvent = nameof(QueuingEvent);
        public const string InvocableActions = nameof(InvocableActions);
        public const string ShouldExecuteSeparately = nameof(ShouldExecuteSeparately);
        public const string EventKey = nameof(EventKey);

        #endregion

        ICancellationManager GlobalCancellationManager { get; }
        IActivityLogger ActivityLogger { get; }
        ITaskProcessingEngine EventProcessor { get; }
        bool IsConfigured { get; set; } = false;
        HashSet<string> ConfiguredEvents { get; set; }

        public string UniqueIdentifier { get; private set; }
        public CancellationToken GlobalCancellationToken => GlobalCancellationManager.CoreToken;


        public Responsibilities(ICancellationManager manager, ITaskProcessingEngine eventProcessor, IActivityLogger activityLogger)
        {
            GlobalCancellationManager = manager;
            EventProcessor = eventProcessor;
            ActivityLogger = activityLogger;
        }

        public void ConfigureNew(HashSet<string> invocableActionNames = null, string identifier = null)
        {
            UniqueIdentifier = identifier;

            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ResponsibilitiesEntity,
                Event = ConfiguringNew,
                Level = ActivityLogLevel.Debug,
            }
            .With(ActivityParam.New(InvocableActions, invocableActionNames))
            .WithCallerInfo());

            TriggerGlobalCancellation();

            //Clear Procesor Queue of any still Processing Tasks marked Separate/Info disposal
            EventProcessor.Stop();

            //Configure Valid Actions 
            ConfiguredEvents = invocableActionNames ?? new HashSet<string>();

            //Refresh Cancellation Manager Source for a new state 
            GlobalCancellationManager.Refresh();

            //Start background thread to queue up Actions/Tasks and fire them
            EventProcessor.StartProcessing(GlobalCancellationManager.CoreToken);

            IsConfigured = true;
        }

        public TOutput QueueBlockingAction<TOutput>(IAction action, bool executeSeparately = false) where TOutput : class
        {
            CheckIfConfigured();

            var jetton = (action as IJettonExecutor).ReturnJetton();

            jetton.IsBlocking = true;

            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ResponsibilitiesEntity,
                Event = QueuingEvent,
                Level = ActivityLogLevel.Verbose,
            }
            .With(ActivityParam.New(EventKey, jetton.EventKey))
            .With(ActivityParam.New(ShouldExecuteSeparately, executeSeparately))
            .WithCallerInfo());


            if (ConfiguredEvents.Any() && !ConfiguredEvents.Contains(jetton.Action.UniqueName))
            {
                throw new ArgumentException($"Key:{jetton.Action.UniqueName} not registered for configured events");
            }

            if (!executeSeparately)
                action.CancellationManager?.Bind(GlobalCancellationManager.CoreToken);

            jetton.MoveToReady();

            EventProcessor.Enqueue(jetton);

            return jetton.GetResult<TOutput>();
        }

        public void QueueAction(IAction action, bool executeSeparately = false)
        {
            CheckIfConfigured();

            var jetton = (action as IJettonExecutor).ReturnJetton();

            jetton.IsBlocking = false;

            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ResponsibilitiesEntity,
                Event = QueuingEvent,
                Level = ActivityLogLevel.Verbose,
            }
            .With(ActivityParam.New(EventKey, jetton.EventKey))
            .With(ActivityParam.New(ShouldExecuteSeparately, executeSeparately))
            .WithCallerInfo());

            if (ConfiguredEvents.Any() && !ConfiguredEvents.Contains(jetton.Action.UniqueName))
            {
                throw new ArgumentException($"Key:{jetton.Action.UniqueName} not registered for configured events");
            }

            if (!executeSeparately)
                action.CancellationManager?.Bind(GlobalCancellationManager.CoreToken);

            jetton.MoveToReady();

            jetton.FreeBlockingResources();

            EventProcessor.Enqueue(jetton);
        }

        public void TriggerGlobalCancellation()
        {
            //Cancel All Tasks linked to Global Source
            GlobalCancellationManager.TriggerCancellation();
        }

        private void CheckIfConfigured()
        {
            if (!IsConfigured) throw new InvalidOperationException($"{nameof(Responsibilities)} not configured");
        }
    }
}
