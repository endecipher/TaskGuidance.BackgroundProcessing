using ActivityLogger.Logging;
using System;

namespace TaskGuidance.BackgroundProcessing.Actions
{
    public class ActionJetton : IDisposable, IActionJetton
    {
        #region Constants

        public const string SignalDoneCalledOnException = nameof(SignalDoneCalledOnException);
        public const string SignalDoneCalledOnSuccess = nameof(SignalDoneCalledOnSuccess);
        public const string GetResultCalled = nameof(GetResultCalled);
        public const string StatusChanged = nameof(StatusChanged);
        public const string JettonUniqueIdentifier = nameof(UniqueIdentifier);
        public const string JettonUniqueActionName = "UniqueActionName";
        public const string OldStatus = nameof(OldStatus);
        public const string NewStatus = nameof(NewStatus);
        public const string ActionJettonEntity = nameof(ActionJetton);

        #endregion

        Guid UniqueIdentifier { get; }
        IActivityLogger ActivityLogger { get; set; }
        public IAction Action { get; }
        public IActionLock Awaiter { get; private set; }
        public bool IsBlocking { get; set; }

        public ActionJetton(IAction action)
        {
            UniqueIdentifier = Guid.NewGuid();
            Action = action;
            Awaiter = new ActionLock(canReset: false);
        }

        public ActionJetton WithLogger(IActivityLogger activityLogger)
        {
            ActivityLogger = activityLogger;
            return this;
        }

        public string EventKey => string.Concat(Action.UniqueName, UniqueIdentifier.ToString());

        public void Dispose()
        {
            Exception = null;
            Result = null;
        }

        public object Result { private get; set; } = null;

        public Exception Exception { get; set; }

        public void SetResultIfAny<T>(T result, Exception encouteredException = null) where T : class
        {
            if (encouteredException != null)
            {
                Exception = encouteredException;

                ActivityLogger?.Log(new Logging.GuidanceActivity
                {
                    EntitySubject = ActionJettonEntity,
                    Event = SignalDoneCalledOnException,
                    Level = ActivityLogLevel.Verbose,
                }
                .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
                .With(ActivityParam.New(JettonUniqueActionName, Action.UniqueName))
                .WithCallerInfo());
            }
            else
            {
                Result = result;

                ActivityLogger?.Log(new Logging.GuidanceActivity
                {
                    EntitySubject = ActionJettonEntity,
                    Event = SignalDoneCalledOnSuccess,
                    Level = ActivityLogLevel.Verbose,
                }
                .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
                .With(ActivityParam.New(JettonUniqueActionName, Action.UniqueName))
                .WithCallerInfo());
            }

            Awaiter?.SignalDone();
        }

        public T GetResult<T>() where T : class
        {
            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                Description = $"GetResult<{nameof(T)}> called",
                EntitySubject = ActionJettonEntity,
                Event = GetResultCalled,
                Level = ActivityLogLevel.Verbose,
            }
            .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
            .With(ActivityParam.New(JettonUniqueActionName, Action.UniqueName))
            .WithCallerInfo());

            Awaiter?.Wait();

            FreeBlockingResources();

            if ((HasCompleted || HasSkipped) && Result is T result)
            {
                return result;
            }

            throw Exception ?? new InvalidOperationException($"Improper Casting to {nameof(T)}/Output Retrieval Failure");
        }

        public void FreeBlockingResources()
        {
            if (!IsBlocking)
            {
                Awaiter?.Dispose();
                Awaiter = null;
            }
        }

        #region Status Manipulations

        private ActionStatusValues Status = ActionStatusValues.New;

        public bool HasFaulted => Status == ActionStatusValues.Faulted;

        public bool HasCanceled => Status == ActionStatusValues.Cancelled;

        public bool HasCompleted => Status == ActionStatusValues.Completed;

        public bool HasSkipped => Status == ActionStatusValues.Skipped;

        public bool HasTimedOut => Status == ActionStatusValues.TimedOut;

        public bool IsProcessing => Status == ActionStatusValues.Processing;


        public void MoveToReady()
        {
            Change(ActionStatusValues.New);
        }

        public void MoveToCompleted()
        {
            Change(ActionStatusValues.Completed);
        }

        public void MoveToSkipped()
        {
            Change(ActionStatusValues.Skipped);
        }

        public void MoveToFaulted()
        {
            Change(ActionStatusValues.Faulted);
        }

        public void MoveToProcessing()
        {
            Change(ActionStatusValues.Processing);
        }

        public void MoveToTimeOut()
        {
            Change(ActionStatusValues.TimedOut);
        }

        public void MoveToCancelled()
        {
            Change(ActionStatusValues.Cancelled);
        }

        public void MoveToStopped()
        {
            Change(ActionStatusValues.Stopped);
            Action.CancellationManager.TriggerCancellation();
        }

        private void Change(ActionStatusValues newValue)
        {
            ActivityLogger?.Log(new Logging.GuidanceActivity
            {
                EntitySubject = ActionJettonEntity,
                Event = StatusChanged,
                Level = ActivityLogLevel.Verbose,
            }
            .With(ActivityParam.New(JettonUniqueIdentifier, UniqueIdentifier.ToString()))
            .With(ActivityParam.New(JettonUniqueActionName, Action.UniqueName))
            .With(ActivityParam.New(OldStatus, Status.ToString()))
            .With(ActivityParam.New(NewStatus, newValue.ToString()))
            .WithCallerInfo());

            Status = newValue;
        }

        #endregion
    }
}
