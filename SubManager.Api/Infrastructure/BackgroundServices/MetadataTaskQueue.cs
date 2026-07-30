using System.Threading.Channels;

namespace SubManager.Api.Infrastructure.BackgroundServices;

public sealed class MetadataTaskQueue : IMetadataTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait});
    private int pendingWorkItemCount;

    public bool HasPendingWork => Volatile.Read(ref pendingWorkItemCount) > 0;

    public async Task QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        Interlocked.Increment(ref pendingWorkItemCount);

        try
        {
            await _queue.Writer.WriteAsync(workItem);
        }
        catch
        {
            MarkCompleted();
            throw;
        }
    }

    public void MarkCompleted() => Interlocked.Decrement(ref pendingWorkItemCount);

    public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
