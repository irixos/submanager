namespace SubManager.Api.Infrastructure.BackgroundServices;

public interface IMetadataTaskQueue
{
    bool HasPendingWork { get; }

    Task QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, Task> workItem);

    void MarkCompleted();

    Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken ct);
}
