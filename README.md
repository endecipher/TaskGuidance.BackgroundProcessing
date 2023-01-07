<img alt="packageIcon" src="https://github.com/endecipher/TaskGuidance.BackgroundProcessing/blob/main/packageIcon.png" width=20% height=20%> 

# TaskGuidance.BackgroundProcessing

Task Processing Engine which controls asynchronous, priority-based, output-optional actions/tasks for .NET

[![NuGet version (TaskGuidance.BackgroundProcessing)](https://img.shields.io/nuget/v/TaskGuidance.BackgroundProcessing.svg?style=flat-square)](https://www.nuget.org/packages/TaskGuidance.BackgroundProcessing/)

## Features
- Enqueue multiple executable actions concurrently 
- Actions support priority levels (Higher priority tasks will be started first)
- Actions support timeouts
- Supports Non-Blocking Actions (Actions which do not return an output)
- Supports Blocking Actions (Caller thread waits for the desired output from the action processed in the background) 
- Filtration; Explicitly control a set of actions which can proceed for execution
- Global Cancellation (Stops and cancels all ongoing background actions/tasks)  
- Optional Internal logging
    - Control Log Level and integrate extensible Log sink

## Documentation

`IResponsibilities` exposes `IResponsibilities.ConfigureNew`, which internally initializes the `TaskProcessingEngine`.
You may supply additional optional parameters during setup for filtration.

`IResponsibilities.ConfigureNew` cancels all attached in-progress background actions, stops the actions which have not yet started, re-configures possible invocable actions (optional parameter), and restarts both `TaskProcessingEngine` and itself.

`TOutput IResponsibilities.QueueBlockingAction<TOutput>` enqueues blocking actions, schedules them for background execution, and finally waits for the output. `ManualResetEventSlim` is internally used to release the calling thread, waiting for the output.

`void IResponsibilities.QueueAction` enqueues non-blocking actions and schedules them for background execution.

The `IResponsibilities` implicitly uses and controls an `TaskProcessingEngine` which has
- A Concurrent Priority Queue for deciding which actions/tasks to be dequeued and started first
- An always-running global Task-Processing logic; which dequeues actions/tasks and executes them asynchronously (i.e in the background).

Any actions/tasks to be enqueued must inherit from the abstract class `BaseAction<TInput, TOutput>` which has
- Its own cancellation capabilities (`ICancellationManager`)
- A `TInput` Input and an optional `TOutput` output
- A `TimeSpan` TimeOut, for throwing timeout exceptions if the `BaseAction` execution exceeds the specified value
- An `ActionPriorityValues` enum which holds the priority for scheduling and execution
- A `string` UniqueName, which helps filtering actions from enqueueing

`BaseAction` internally contains a well-defined workflow which not only wraps the overridden custom logic, but also extends exception handling capabilities. 
When an `BaseAction` is started from the `TaskProcessingEngine`, the following occurs in order:

1. **`TOutput BaseAction<TInput, TOutput>.DefaultOutput`** - Initialization of default output    
2. **`Task<bool> BaseAction<TInput, TOutput>.ShouldProceed`** - Check to determine whether the core logic should proceed
3. **`Task<TOutput> BaseAction<TInput, TOutput>.Action`** - Core Logic
4. **`Task BaseAction<TInput, TOutput>.PostAction`** - Post-processing logic
5. **`Task BaseAction<TInput, TOutput>.OnActionEnd`** - Additional logic after returning result

####
Custom handling of errors/exceptions during processing is also available.

- **`Task BaseAction<TInput, TOutput>.OnTimeOut`** - If Core logic times out
- **`Task BaseAction<TInput, TOutput>.OnCancellation`** - If Cancellation is Requested
- **`Task BaseAction<TInput, TOutput>.OnFailure`** - If an error or a failure occurs
####


### DI Registration

You can either register manually the following dependencies:

```C#
RegisterTransient<ICancellationManager, CancellationManager>();
RegisterSingleton<IResponsibilities, Responsibilities>();
RegisterSingleton<ITaskProcessingEngine, TaskProcessingEngine>();
RegisterSingleton<ITaskProcessorConfiguration, TaskProcessorConfiguration>();
```

Or you can wrap your chosen DI Container under `IDependencyContainer` and call `GuidanceDependencyRegistration.Register(IDependencyContainer)` to implicitly register all dependencies during startup.

```C# 
//Example
public class DependencyContainer : TaskGuidance.BackgroundProcessing.Dependencies.IDependencyContainer
{
    IServiceCollection ServiceDescriptors { get; }

    void IDependencyContainer.RegisterSingleton<T1, T2>()
    {
        ServiceDescriptors.AddSingleton<T1, T2>();
    }

    .
    .
```

### Configuration Settings

Configure `ITaskProcessorConfiguration` during startup:

#### `ITaskProcessorConfiguration.ProcessorQueueSize`
- Integer - Denotes the Action Queue Initial Capacity. Default is 100 if not specified. 

#### `ITaskProcessorConfiguration.ProcessorWaitTimeWhenQueueEmpty_InMilliseconds`
- Integer - Control the Aggression of checking new Actions to process if Queue is found empty. Default is 1000ms / 1s if not specified


## Example Usage 

```C#
// Define an action 
public class SomeAction : BaseAction<InputData, OutputData>
{
    /* Override logic and define properties */
}

// From DI..
IResponsibilities Responsibilities; 

// Configure 
Responsibilities.ConfigureNew(invocableActionNames: null);

// Create an instance of the action 
var someAction = new SomeAction(new InputData 
{
     
});

// Support Cancellation and binding capabilities 
someAction.SupportCancellation();

// Retrieve output (for blocking actions)
OutputData output = Responsibilities.QueueBlockingAction<OutputData>(someAction, executeSeparately: false);

```



## Contributing

Contributions are always welcome!



## Authors

- [@endecipher](https://www.github.com/endecipher)


## License

[MIT](https://choosealicense.com/licenses/mit/)