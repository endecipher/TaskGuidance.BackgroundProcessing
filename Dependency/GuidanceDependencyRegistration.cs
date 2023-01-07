using EventGuidance.Cancellation;
using EventGuidance.Logging;
using EventGuidance.Responsibilities;
using EventGuidance.Structure;

namespace EventGuidance.Dependency
{
    public class GuidanceDependencyRegistration : IDependencyRegistration
    {
        public void Register(IDependencyContainer container)
        {
            container.RegisterTransient<ICancellationManager, CancellationManager>();
            container.RegisterTransient<IActionLock, ActionLock>();
            container.RegisterTransient<IConcurrentAwaiter, ConcurrentAwaiter>();
            container.RegisterSingleton<IResponsibilities, Responsibilities.Responsibilities>();
            container.RegisterSingleton<IEventProcessor, EventProcessor>();
            container.RegisterSingleton<IEventProcessorConfiguration, EventProcessorConfiguration>();
        }
    }
}
