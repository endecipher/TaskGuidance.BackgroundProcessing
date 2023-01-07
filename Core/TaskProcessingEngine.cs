using ActivityLogger.Logging;
using System.Threading;
using System.Threading.Tasks;
using TaskGuidance.BackgroundProcessing.Actions;
using TaskGuidance.BackgroundProcessing.Collections;

namespace TaskGuidance.BackgroundProcessing.Core
{
    public class TaskProcessingEngine : ITaskProcessingEngine
    {
        #region Constants

        public const string ProcessorEntity = nameof(TaskProcessingEngine);
        public const string Starting = nameof(Starting);
        public const string Waiting = nameof(Waiting);
        public const string Stopping = nameof(Stopping);
        public const string Dequeued = nameof(Dequeued);
        public const string ForceStopping = nameof(ForceStopping);
        public const string EventKey = nameof(EventKey);

        #endregion

        IProcessorConfiguration ProcessorConfiguration { get; }

        ConcurrentPriorityQueue<IActionJetton, ActionPriorityValues> ConcurrentPriorityQueue { get; }

        IActivityLogger ActivityLogger { get; }

        public TaskProcessingEngine(IProcessorConfiguration processorConfiguration, IActivityLogger activityLogger)
        {
            ProcessorConfiguration = processorConfiguration;
            ActivityLogger = activityLogger;

            ConcurrentPriorityQueue = new ConcurrentPriorityQueue<IActionJetton, ActionPriorityValues>(
                ProcessorConfiguration.ProcessorQueueSize,
                new ActionPriorityComparer()
            );
        }

        public void StartProcessing(CancellationToken token)
        {
            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ProcessorEntity,
                Event = Starting,
                Level = ActivityLogLevel.Verbose,

            }
            .WithCallerInfo());

            var process = new Task(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    if (ConcurrentPriorityQueue.TryDequeue(out var info, out var priority))
                    {
                        ActivityLogger?.Log(new Logging.GuidanceActivity
                        {
                            Description = $"Dequeued and processing {info.EventKey}",
                            EntitySubject = ProcessorEntity,
                            Event = Dequeued,
                            Level = ActivityLogLevel.Verbose,

                        }
                        .With(ActivityParam.New(EventKey, info.EventKey))
                        .WithCallerInfo());

                        new Task(() => (info.Action as IJettonExecutor).Perform(info), token, TaskCreationOptions.AttachedToParent).Start();
                    }
                    else
                    {
                        ActivityLogger?.Log(new Logging.GuidanceActivity
                        {
                            EntitySubject = ProcessorEntity,
                            Event = Waiting,
                            Level = ActivityLogLevel.Verbose,

                        }
                        .WithCallerInfo());

                        Task.Delay(ProcessorConfiguration.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds, token).Wait();
                    }
                }
            },
            cancellationToken: token,
            creationOptions: TaskCreationOptions.DenyChildAttach);

            process.Start();
        }

        public void Stop()
        {
            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ProcessorEntity,
                Event = Stopping,
                Level = ActivityLogLevel.Verbose,

            }
            .WithCallerInfo());

            while (ConcurrentPriorityQueue.TryDequeue(out var info, out var priority))
            {
                info.MoveToStopped();

                ActivityLogger?.Log(new Logging.GuidanceActivity
                {
                    Description = $"Dequeued and force-stopping {info.EventKey}",
                    EntitySubject = ProcessorEntity,
                    Event = ForceStopping,
                    Level = ActivityLogLevel.Verbose,

                }
                .With(ActivityParam.New(EventKey, info.EventKey))
                .WithCallerInfo());

                info.Dispose();
            }
        }

        public void Enqueue(IActionJetton info)
        {
            ConcurrentPriorityQueue.Enqueue(info, info.Action.PriorityValue);
        }
    }
}
