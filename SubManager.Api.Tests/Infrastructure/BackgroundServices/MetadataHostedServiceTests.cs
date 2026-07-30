using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SubManager.Api.Infrastructure.BackgroundServices;
using Xunit;

namespace SubManager.Api.Tests.Infrastructure.BackgroundServices;

public sealed class MetadataHostedServiceTests
{
    [Fact]
    public async Task Processing_FailedItem_MarksCompletionAndContinues()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var queue = new RecordingTaskQueue(expectedCompletions: 2);
        var secondRan = false;
        await queue.QueueBackgroundWorkItemAsync(
            (_, _) => throw new InvalidOperationException("Expected failure"));
        await queue.QueueBackgroundWorkItemAsync((_, _) =>
        {
            secondRan = true;
            return Task.CompletedTask;
        });
        var service = new MetadataHostedService(
            queue,
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<MetadataHostedService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await queue.AllCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await service.StopAsync(CancellationToken.None);

        Assert.True(secondRan);
        Assert.Equal(2, queue.CompletedCount);
        Assert.False(queue.HasPendingWork);
    }

    private sealed class RecordingTaskQueue(int expectedCompletions) : IMetadataTaskQueue
    {
        private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> channel =
            Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();
        private int pending;

        public TaskCompletionSource AllCompleted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CompletedCount { get; private set; }

        public bool HasPendingWork => pending > 0;

        public async Task QueueBackgroundWorkItemAsync(
            Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            Interlocked.Increment(ref pending);
            await channel.Writer.WriteAsync(workItem);
        }

        public void MarkCompleted()
        {
            Interlocked.Decrement(ref pending);
            CompletedCount++;
            if (CompletedCount == expectedCompletions) AllCompleted.TrySetResult();
        }

        public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(
            CancellationToken ct)
        {
            return await channel.Reader.ReadAsync(ct);
        }
    }
}
