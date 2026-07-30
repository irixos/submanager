using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Videos.Services;
using SubManager.Api.Infrastructure.BackgroundServices;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Application.Features.Videos.Services;

public sealed class VideoServiceTests
{
    [Fact]
    public async Task GetDurationStatusAsync_RequestedIds_ReturnsProjectionAndQueueState()
    {
        using var database = new SqliteTestDatabase();
        var queue = new MetadataTaskQueue();
        var channel = new Channel
        {
            YoutubeChannelId = "channel",
            Name = "Channel",
            AddedDate = DateTimeOffset.UtcNow
        };
        database.Context.AddRange(
            new Video
            {
                YoutubeVideoId = "first",
                Title = "First",
                Channel = channel,
                PublishedDate = DateTimeOffset.UtcNow,
                AddedDate = DateTimeOffset.UtcNow,
                MetadataLastRefreshedAt = DateTimeOffset.UtcNow,
                DurationSeconds = 90
            },
            new Video
            {
                YoutubeVideoId = "second",
                Title = "Second",
                Channel = channel,
                PublishedDate = DateTimeOffset.UtcNow,
                AddedDate = DateTimeOffset.UtcNow,
                MetadataLastRefreshedAt = DateTimeOffset.UtcNow
            },
            new Video
            {
                YoutubeVideoId = "excluded",
                Title = "Excluded",
                Channel = channel,
                PublishedDate = DateTimeOffset.UtcNow,
                AddedDate = DateTimeOffset.UtcNow,
                MetadataLastRefreshedAt = DateTimeOffset.UtcNow,
                DurationSeconds = 120
            });
        await database.Context.SaveChangesAsync();
        var videos = await database.Context.Videos.OrderBy(video => video.Id).ToListAsync();
        var service = new VideoService(database.Context, queue);

        var idleResult = await service.GetDurationStatusAsync(
            [videos[1].Id, videos[0].Id, 999],
            CancellationToken.None);

        Assert.False(idleResult.HasPendingMetadata);

        await queue.QueueBackgroundWorkItemAsync((_, _) => Task.CompletedTask);
        var result = await service.GetDurationStatusAsync(
            [videos[1].Id, videos[0].Id, 999],
            CancellationToken.None);

        Assert.True(result.HasPendingMetadata);
        Assert.Equal(2, result.Videos.Count);
        Assert.Contains(result.Videos, video =>
            video.Id == videos[0].Id && video.DurationSeconds == 90);
        Assert.Contains(result.Videos, video =>
            video.Id == videos[1].Id && video.DurationSeconds is null);
        Assert.DoesNotContain(result.Videos, video => video.Id == videos[2].Id);
    }
}
