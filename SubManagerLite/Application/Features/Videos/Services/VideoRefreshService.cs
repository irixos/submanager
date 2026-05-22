using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Models;
using SubManagerLite.Application.Interfaces;
using SubManagerLite.Infrastructure;
using SubManagerLite.Infrastructure.BackgroundServices;

namespace SubManagerLite.Application.Features.Videos.Services;

public sealed class VideoRefreshService(
    ApplicationDbContext db,
    IMetadataTaskQueue metadataTaskQueue,
    IYoutubeVideoIngestService youtubeVideoIngestService,
    IYoutubeMetadataProvider youtubeMetadataProvider) : IVideoRefreshService
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

            // get video ids of new videos in recentVideos
            var newVideoIds = await GetNewVideoIdsAsync(recentVideos
                .OrderByDescending(v => v.PublishedDate)
                .Select(v => v.YoutubeVideoId).ToList(), ct);

            // upsert new videos
            await UpsertRangeAsync(recentVideos, ct);

            // queue background task to refresh metadata for new videos
            await metadataTaskQueue.QueueBackgroundWorkItemAsync(async (sp, cancellationToken) =>
            {
                var videoRefreshService = sp.GetRequiredService<IVideoRefreshService>();
                
                await videoRefreshService.RefreshMetadataForVideosAsync(newVideoIds, cancellationToken);
            });

            // get newly refreshed videos
            var youtubeVideoIds = recentVideos
                .Select(v => v.YoutubeVideoId)
                .Distinct()
                .ToList();

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
        IReadOnlyCollection<string> newVideoIds, 
        CancellationToken ct)
    {
        var pendingVideoInfos = new Dictionary<string, YoutubeVideoInfo>();

        foreach (var videoId in newVideoIds)
        {
            var videoInfo = await youtubeMetadataProvider.GetVideoInfo(videoId, ct);

            pendingVideoInfos[videoId] = videoInfo;

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

    private async Task<List<string>> GetNewVideoIdsAsync(List<string> youtubeVideoIds, CancellationToken ct)
    {
        var existingVideoIds = await db.Videos
            .Where(v => youtubeVideoIds.Contains(v.YoutubeVideoId))
            .Select(v => v.YoutubeVideoId)
            .ToListAsync(ct);
        
        var existingVideoIdsSet = existingVideoIds.ToHashSet();
        
        return youtubeVideoIds
            .Where(id => !existingVideoIdsSet.Contains(id))
            .ToList();       
    }
    
    private async Task UpsertRangeAsync(List<Video> videos, CancellationToken ct)
    {
        var addedDate = DateTimeOffset.UtcNow;
        
        foreach (var video in videos)
            video.AddedDate = addedDate;       
        
        foreach (var batch in videos.Chunk(MaxUpsertBatchSize))
        {
            await db.Videos
                .UpsertRange(batch)
                .On(v => v.YoutubeVideoId)
                .Exclude(v => v.AddedDate)
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