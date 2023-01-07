namespace TaskGuidance.BackgroundProcessing.Core
{
    public class EventProcessorConfiguration : IProcessorConfiguration
    {
        public int ProcessorQueueSize { get; set; } = 100;

        public int ProcessorWaitTimeWhenQueueEmpty_InMilliseconds { get; set; } = 1000;
    }
}
