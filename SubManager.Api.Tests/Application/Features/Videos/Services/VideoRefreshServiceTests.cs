using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Videos.Interfaces;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Application.Features.Videos.Services;
using SubManager.Api.Application.Interfaces;
using SubManager.Api.Infrastructure.BackgroundServices;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Application.Features.Videos.Services;

public sealed class VideoRefreshServiceTests
{
    [Fact]
    public async Task RefreshMetadataForVideosAsync_MixedState_ProcessesNewestFirstAndPersistsSuccessfulBatches()
    {
        var saveInterceptor = new SaveChangesCounter();
        using var database = new SqliteTestDatabase(saveInterceptor);
        var published = DateTimeOffset.UtcNow;
        var channel = CreateChannel();
        var videos = new[]
        {
            CreateVideo(1, channel, published.AddMinutes(-4)),
            CreateVideo(2, channel, published.AddMinutes(-2)),
            CreateVideo(3, channel, published.AddMinutes(-2)),
            CreateVideo(4, channel, published.AddMinutes(-1)),
            CreateVideo(5, channel, published.AddMinutes(1), durationSeconds: 500),
            CreateVideo(6, channel, published.AddMinutes(2))
        };
        database.Context.AddRange(videos);
        await database.Context.SaveChangesAsync();
        saveInterceptor.Reset();
        var provider = new RecordingMetadataProvider("video-4");
        var service = CreateService(database.Context, provider);

        await service.RefreshMetadataForVideosAsync(
            ["video-2", "video-5", "video-1", "video-6", "video-4", "video-3"],
            CancellationToken.None);

        Assert.Equal(["video-6", "video-4", "video-3", "video-2", "video-1"], provider.RequestedIds);
        Assert.Equal(2, saveInterceptor.SavingChangesCount);
        database.Context.ChangeTracker.Clear();
        var updated = await database.Context.Videos
            .OrderBy(video => video.Id)
            .ToListAsync();
        Assert.Equal(10, updated[0].DurationSeconds);
        Assert.Equal(20, updated[1].DurationSeconds);
        Assert.Equal(30, updated[2].DurationSeconds);
        Assert.Null(updated[3].DurationSeconds);
        Assert.Equal(500, updated[4].DurationSeconds);
        Assert.Equal(60, updated[5].DurationSeconds);
    }

    [Fact]
    public async Task RefreshAllAsync_ExistingVideo_PreservesLocallyOwnedMetadata()
    {
        using var database = new SqliteTestDatabase();
        var channel = CreateChannel();
        var addedDate = DateTimeOffset.UtcNow.AddDays(-5);
        var watchedDate = DateTimeOffset.UtcNow.AddDays(-1);
        var existing = CreateVideo(
            1,
            channel,
            DateTimeOffset.UtcNow.AddHours(-1),
            durationSeconds: 125);
        existing.Title = "Old title";
        existing.AddedDate = addedDate;
        existing.WatchedDate = watchedDate;
        database.Context.Add(existing);
        await database.Context.SaveChangesAsync();
        var incoming = CreateVideo(
            0,
            channel,
            existing.PublishedDate,
            durationSeconds: null);
        incoming.Channel = null!;
        incoming.ChannelId = channel.Id;
        incoming.YoutubeVideoId = existing.YoutubeVideoId;
        incoming.Title = "Updated title";
        incoming.AddedDate = default;
        incoming.WatchedDate = null;
        var ingest = new StubVideoIngestService([incoming]);
        var service = CreateService(
            database.Context,
            new RecordingMetadataProvider(),
            ingest);

        await service.RefreshAllAsync(CancellationToken.None);

        database.Context.ChangeTracker.Clear();
        var updated = await database.Context.Videos.SingleAsync();
        Assert.Equal("Updated title", updated.Title);
        Assert.Equal(addedDate, updated.AddedDate);
        Assert.Equal(watchedDate, updated.WatchedDate);
        Assert.Equal(125, updated.DurationSeconds);
    }

    private static VideoRefreshService CreateService(
        SubManager.Api.Infrastructure.ApplicationDbContext db,
        IYoutubeMetadataProvider metadataProvider,
        IYoutubeVideoIngestService? ingestService = null)
    {
        return new VideoRefreshService(
            db,
            new MetadataTaskQueue(),
            ingestService ?? new StubVideoIngestService([]),
            metadataProvider,
            NullLogger<VideoRefreshService>.Instance);
    }

    private static Channel CreateChannel()
    {
        return new Channel
        {
            YoutubeChannelId = $"channel-{Guid.NewGuid():N}"[..24],
            Name = "Channel",
            AddedDate = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    private static Video CreateVideo(
        int id,
        Channel channel,
        DateTimeOffset publishedDate,
        int? durationSeconds = null)
    {
        return new Video
        {
            Id = id,
            YoutubeVideoId = $"video-{id}",
            Channel = channel,
            Title = $"Video {id}",
            PublishedDate = publishedDate,
            AddedDate = DateTimeOffset.UtcNow,
            MetadataLastRefreshedAt = DateTimeOffset.UtcNow,
            DurationSeconds = durationSeconds
        };
    }

    private sealed class RecordingMetadataProvider(params string[] failingIds) : IYoutubeMetadataProvider
    {
        private readonly HashSet<string> failingIds = [.. failingIds];

        public List<string> RequestedIds { get; } = [];

        public Task<YoutubeChannelInfo> GetChannelInfo(
            YoutubeChannelRef youtubeChannelRef,
            CancellationToken ct) => throw new NotSupportedException();

        public Task<YoutubeVideoInfo> GetVideoInfo(string videoId, CancellationToken ct)
        {
            RequestedIds.Add(videoId);

            if (failingIds.Contains(videoId))
                throw new InvalidOperationException("Metadata unavailable.");

            var id = int.Parse(videoId["video-".Length..]);
            return Task.FromResult(new YoutubeVideoInfo(id * 10));
        }
    }

    private sealed class StubVideoIngestService(IReadOnlyCollection<Video> videos)
        : IYoutubeVideoIngestService
    {
        public Task<IReadOnlyCollection<Video>> GetRecentVideosAsync(
            IReadOnlyCollection<Channel> channels,
            CancellationToken ct) => Task.FromResult(videos);
    }

    private sealed class SaveChangesCounter : SaveChangesInterceptor
    {
        public int SavingChangesCount { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            SavingChangesCount++;
            return ValueTask.FromResult(result);
        }

        public void Reset() => SavingChangesCount = 0;
    }
}
