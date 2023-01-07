using TaskGuidance.BackgroundProcessing.Cancellation;
using TaskGuidance.BackgroundProcessing.Core;

namespace TaskGuidance.BackgroundProcessing.Dependencies
{
    public interface IDependencyRegistration
    {
        void Register(IDependencyContainer container);
    }

    public class GuidanceDependencyRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterTransient<ICancellationManager, CancellationManager>();
            container.RegisterSingleton<IResponsibilities, Responsibilities>();
            container.RegisterSingleton<ITaskProcessingEngine, TaskProcessingEngine>();
            container.RegisterSingleton<ITaskProcessorConfiguration, TaskProcessorConfiguration>();
        }
    }
}
