namespace SubManagerLite.Infrastructure.BackgroundServices;

public interface IMetadataTaskQueue
{
    Task QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, Task> workItem);
    
    Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken ct);
}