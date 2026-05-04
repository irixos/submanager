using System.Threading.Channels;

namespace SubManagerLite.Infrastructure.BackgroundServices;

public sealed class MetadataTaskQueue : IMetadataTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(
        new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.Wait});

    public async Task QueueBackgroundWorkItemAsync(Func<IServiceProvider, CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await _queue.Writer.WriteAsync(workItem);
    }

    public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}