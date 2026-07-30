using Gridify;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Application.Features.Videos.Services;
using SubManager.Api.Infrastructure.BackgroundServices;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Application.Features.Videos.Services;

public sealed class VideoServiceTests
{
    [Fact]
    public async Task GetAllAndGetAsync_ExistingVideo_ReturnNestedProjection()
    {
        using var database = new SqliteTestDatabase();
        var category = new Category { Name = "Technology", Color = "#123456" };
        var channel = CreateChannel();
        channel.Categories.Add(category);
        var video = CreateVideo(channel, "video-projection", "Projection");
        database.Context.Add(video);
        await database.Context.SaveChangesAsync();
        var service = new VideoService(database.Context, new MetadataTaskQueue());

        var page = await service.GetAllAsync(
            new GridifyQuery { Page = 1, PageSize = 10 },
            CancellationToken.None);
        var found = await service.GetAsync(video.Id, CancellationToken.None);
        var missing = await service.GetAsync(video.Id + 1, CancellationToken.None);

        Assert.Equal(1, page.Count);
        var item = Assert.Single(page.Data);
        Assert.Equal("Projection", item.Title);
        Assert.Equal(channel.Id, item.Channel.Id);
        Assert.Equal("Technology", Assert.Single(item.Categories).Name);
        Assert.Equal(video.Id, found?.Id);
        Assert.Null(missing);
    }

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

    [Fact]
    public async Task GetDurationStatusAsync_EmptySelection_ReturnsQueueStateWithoutVideos()
    {
        using var database = new SqliteTestDatabase();
        var queue = new MetadataTaskQueue();
        await queue.QueueBackgroundWorkItemAsync((_, _) => Task.CompletedTask);
        var service = new VideoService(database.Context, queue);

        var result = await service.GetDurationStatusAsync([], CancellationToken.None);

        Assert.True(result.HasPendingMetadata);
        Assert.Empty(result.Videos);
    }

    [Fact]
    public async Task UpdateWatchedDateAsync_SetClearAndMissing_UpdatePersistedState()
    {
        using var database = new SqliteTestDatabase();
        var video = CreateVideo(CreateChannel(), "video-watched", "Watched");
        database.Context.Add(video);
        await database.Context.SaveChangesAsync();
        var service = new VideoService(database.Context, new MetadataTaskQueue());
        var watchedDate = DateTimeOffset.UtcNow.AddMinutes(-1);

        Assert.True(await service.UpdateWatchedDateAsync(
            video.Id,
            new UpdateVideoWatchedDateRequest { WatchedDate = watchedDate },
            CancellationToken.None));
        Assert.Equal(watchedDate, video.WatchedDate);
        Assert.True(video.IsWatched);

        Assert.True(await service.UpdateWatchedDateAsync(
            video.Id,
            new UpdateVideoWatchedDateRequest { WatchedDate = null },
            CancellationToken.None));
        Assert.Null(video.WatchedDate);
        Assert.False(video.IsWatched);

        Assert.False(await service.UpdateWatchedDateAsync(
            video.Id + 1,
            new UpdateVideoWatchedDateRequest(),
            CancellationToken.None));
    }

    private static Channel CreateChannel()
    {
        return new Channel
        {
            YoutubeChannelId = $"UC-{Guid.NewGuid():N}"[..24],
            Name = "Channel",
            AddedDate = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    private static Video CreateVideo(Channel channel, string youtubeId, string title)
    {
        return new Video
        {
            YoutubeVideoId = youtubeId,
            Channel = channel,
            Title = title,
            PublishedDate = DateTimeOffset.UtcNow,
            AddedDate = DateTimeOffset.UtcNow,
            MetadataLastRefreshedAt = DateTimeOffset.UtcNow
        };
    }
}
