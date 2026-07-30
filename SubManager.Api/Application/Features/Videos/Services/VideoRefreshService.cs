using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Videos.Interfaces;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Application.Interfaces;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.BackgroundServices;

namespace SubManager.Api.Application.Features.Videos.Services;

public sealed class VideoRefreshService(
    ApplicationDbContext db,
    IMetadataTaskQueue metadataTaskQueue,
    IYoutubeVideoIngestService youtubeVideoIngestService,
    IYoutubeMetadataProvider youtubeMetadataProvider,
    ILogger<VideoRefreshService> logger) : IVideoRefreshService
{
    private const int DefaultPageSize = 25;
    private const int MetadataUpdateBatchSize = 3;
    private const int MaxUpsertBatchSize = 200;
    private static readonly SemaphoreSlim RefreshSemaphore = new(1, 1);
    
    public async Task<RefreshResult> RefreshAllAsync(CancellationToken ct)
    {
        // only allow one refresh at a time
        if (!await RefreshSemaphore.WaitAsync(0, ct))
            return new RefreshResult {IsAlreadyRunning = true};

        try
        {
            // get all active channels
            var activeChannels = await db.Channels
                .Where(c => c.IsActive)
                .ToListAsync(ct);

            // get recent videos for each channel
            var recentVideos = await youtubeVideoIngestService.GetRecentVideosAsync(activeChannels, ct);

            // update channel last checked date
            await db.SaveChangesAsync(ct);

            if (recentVideos.Count == 0)
                return new RefreshResult();

            // upsert new videos
            await UpsertRangeAsync(recentVideos, ct);

            var youtubeVideoIds = recentVideos
                .Select(video => video.YoutubeVideoId)
                .Distinct()
                .ToList();
            var pendingVideoIds = await GetPendingVideoIdsAsync(youtubeVideoIds, ct);

            if (pendingVideoIds.Count > 0)
            {
                await metadataTaskQueue.QueueBackgroundWorkItemAsync(async (sp, cancellationToken) =>
                {
                    var videoRefreshService = sp.GetRequiredService<IVideoRefreshService>();

                    await videoRefreshService.RefreshMetadataForVideosAsync(pendingVideoIds, cancellationToken);
                });
            }

            // return new feed items
            var response = new RefreshResult
            {
                Response = await db.Videos
                    .Where(v => youtubeVideoIds.Contains(v.YoutubeVideoId))
                    .Select(VideoMappings.ToVideoResponse)
                    .OrderByDescending(v => v.PublishedDate)
                    .Take(DefaultPageSize)
                    .ToListAsync(ct)
            };

            return response;
        }
        finally
        {
            RefreshSemaphore.Release();
        }
    }

    public async Task RefreshMetadataForVideosAsync(
        IReadOnlyCollection<string> videoIds,
        CancellationToken ct)
    {
        var pendingVideoIds = await GetPendingVideoIdsAsync(videoIds, ct);
        var pendingVideoInfos = new Dictionary<string, YoutubeVideoInfo>();

        foreach (var videoId in pendingVideoIds)
        {
            try
            {
                var videoInfo = await youtubeMetadataProvider.GetVideoInfo(videoId, ct);
                pendingVideoInfos[videoId] = videoInfo;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Unable to refresh metadata for video {YoutubeVideoId}", videoId);
                continue;
            }

            if (pendingVideoInfos.Count >= MetadataUpdateBatchSize)
            {
                await UpdateMetadataAsync(pendingVideoInfos, ct);
                pendingVideoInfos.Clear();
            }
        }

        // flush remaining pending videos
        if (pendingVideoInfos.Count == 0) return;
        
        await UpdateMetadataAsync(pendingVideoInfos, ct);
    }

    private async Task<IReadOnlyList<string>> GetPendingVideoIdsAsync(
        IReadOnlyCollection<string> youtubeVideoIds,
        CancellationToken ct)
    {
        return await db.Videos
            .Where(video =>
                youtubeVideoIds.Contains(video.YoutubeVideoId) &&
                video.DurationSeconds == null)
            .OrderByDescending(video => video.PublishedDate)
            .ThenByDescending(video => video.Id)
            .Select(video => video.YoutubeVideoId)
            .ToListAsync(ct);
    }
    
    private async Task UpsertRangeAsync(IReadOnlyCollection<Video> videos, CancellationToken ct)
    {
        var addedDate = DateTimeOffset.UtcNow;
        
        foreach (var video in videos)
            video.AddedDate = addedDate;       
        
        foreach (var batch in videos.Chunk(MaxUpsertBatchSize))
        {
            await db.Videos
                .UpsertRange(batch)
                .On(v => v.YoutubeVideoId)
                .Exclude(v => new { v.AddedDate, v.WatchedDate, v.DurationSeconds })
                .RunAsync(ct);
        }
    }
    
    private async Task UpdateMetadataAsync(Dictionary<string, YoutubeVideoInfo> pendingVideoInfos, CancellationToken ct)
    {
        var pendingVideoInfoKeys = pendingVideoInfos.Keys.ToList();
        
        var videos = await db.Videos
            .Where(v => pendingVideoInfoKeys.Contains(v.YoutubeVideoId))
            .ToListAsync(ct);

        foreach (var video in videos)
        {
            var duration = pendingVideoInfos[video.YoutubeVideoId].DurationSeconds;
            
            video.DurationSeconds = duration;
        }
        
        await db.SaveChangesAsync(ct);       
    }
}
