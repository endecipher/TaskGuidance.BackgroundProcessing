namespace TaskGuidance.BackgroundProcessing.Core
{
    public interface IProcessorConfiguration
    {
        /// <summary>
        /// Action Queue Initial Capacity.
        /// Default is <c>100</c> if not specified
        /// </summary>
        public int ProcessorQueueSize { get; }

        /// <summary>
        /// Control the Aggression of checking new Actions to process if Queue is found empty.
        /// Default is <c>1000ms / 1s</c> if not specified
        /// </summary>
        public int ProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; }
    }
}
