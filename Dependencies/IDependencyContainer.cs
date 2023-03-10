namespace TaskGuidance.BackgroundProcessing.Dependencies
{
    /// <summary>
    /// An implementation of this interface must be supplied to <see cref="IDependencyRegistration.Register(IDependencyContainer)"/> for registering implicitly configured services.
    /// The Dependency Injection solution may vary (i.e .NET DI/StructureMap/Autofac etc), but during setup, the container can be wrapped under <see cref="IDependencyContainer"/> for abstraction.
    /// </summary>
    /// 
    public interface IDependencyContainer
    {
        void RegisterTransient<T1, T2>() where T2 : class, T1 where T1 : class;

        void RegisterSingleton<T1, T2>() where T2 : class, T1 where T1 : class;
    }
}
