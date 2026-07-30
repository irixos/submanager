using SubManager.Api.Infrastructure.BackgroundServices;
using Xunit;

namespace SubManager.Api.Tests.Infrastructure.BackgroundServices;

public sealed class MetadataTaskQueueTests
{
    [Fact]
    public async Task QueueLifecycle_TracksPendingUntilCompletion()
    {
        var queue = new MetadataTaskQueue();
        static Task WorkItem(IServiceProvider _, CancellationToken ct) => Task.CompletedTask;

        await queue.QueueBackgroundWorkItemAsync(WorkItem);

        Assert.True(queue.HasPendingWork);

        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        Assert.True(queue.HasPendingWork);
        Assert.Equal((Func<IServiceProvider, CancellationToken, Task>)WorkItem, dequeued);

        queue.MarkCompleted();

        Assert.False(queue.HasPendingWork);
    }

    [Fact]
    public async Task QueueBackgroundWorkItemAsync_NullWorkItem_ThrowsAndRemainsIdle()
    {
        var queue = new MetadataTaskQueue();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => queue.QueueBackgroundWorkItemAsync(null!));

        Assert.False(queue.HasPendingWork);
    }
}
