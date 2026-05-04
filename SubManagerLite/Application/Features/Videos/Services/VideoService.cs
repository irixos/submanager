using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Models;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Models;
using SubManagerLite.Application.Interfaces;
using SubManagerLite.Infrastructure.BackgroundServices;

namespace SubManagerLite.Application.Features.Videos.Services;

public sealed class VideoService(
    IVideoRepository videoRepository,
    IChannelRepository channelRepository,
    IYoutubeVideoIngestService youtubeVideoIngestService,
    IMetadataTaskQueue metadataTaskQueue) : IVideoService
{
    
   private const int DefaultPageSize = 25;
   private const int MetadataUpdateBatchSize = 3;
    
   public async Task<List<VideoResponse>> GetAllAsync(CancellationToken ct)
    {
        var videos = await videoRepository.GetAllAsync(ct);
        
        var response = videos.Select(MapToVideoResponse).ToList();
        
        return response;
    }

    public async Task<VideoResponse?> GetAsync(int id, CancellationToken ct)
    {
        var video = await videoRepository.GetAsync(id, ct);
        if (video is null) return null;

        var response = MapToVideoResponse(video);

        return response;
    }
    
    public async Task<List<VideoResponse>> RefreshAllAsync(CancellationToken ct)
    {
        // get all active channels
        var activeChannels = await channelRepository.GetAllActiveAsync(ct);
        
        // get recent videos for each channel
        var recentVideos = await youtubeVideoIngestService.GetRecentVideosAsync(activeChannels, ct);

        // update channel last checked date
        await channelRepository.SaveChangesAsync(ct);
        
        if (recentVideos.Count == 0)
            return [];
        
        // get video ids of new videos in recentVideos
        var newVideoIds = await videoRepository.GetNewVideoIdsAsync(recentVideos
            .OrderByDescending(v => v.PublishedDate)
            .Select(v => v.YoutubeVideoId).ToList(), ct);
        
        //upsert new videos
        await videoRepository.UpsertRangeAsync(recentVideos, ct);

        // queue background task to refresh metadata for new videos
        await metadataTaskQueue.QueueBackgroundWorkItemAsync(async (sp, cancellationToken) =>
        {
            var youtubeMetadataProvider = sp.GetRequiredService<IYoutubeMetadataProvider>();
            var backgroundVideoRepository = sp.GetRequiredService<IVideoRepository>();
            
            var pendingVideoInfos = new Dictionary<string, YoutubeVideoInfo>();
            
            foreach (var videoId in newVideoIds)
            {
                var videoInfo = await youtubeMetadataProvider.GetVideoInfo(videoId, cancellationToken);
                
                pendingVideoInfos[videoId] = videoInfo;

                if (pendingVideoInfos.Count >= MetadataUpdateBatchSize)
                {
                    await backgroundVideoRepository.UpdateMetadataAsync(pendingVideoInfos, cancellationToken);
                    pendingVideoInfos.Clear();
                }
            }
            
            // flush remaining pending videos
            if (pendingVideoInfos.Count == 0) return;
            await backgroundVideoRepository.UpdateMetadataAsync(pendingVideoInfos, cancellationToken);
        });
        
        // get newly refreshed videos
        var youtubeVideoIds = recentVideos
            .Select(v => v.YoutubeVideoId)
            .Distinct()
            .ToList();
        
        var refreshedVideos = await videoRepository.GetByYoutubeVideoIdsAsync(youtubeVideoIds, ct);
        
        // return new feed items
        var response = refreshedVideos
            .Select(MapToVideoResponse)
            .OrderByDescending(v => v.PublishedDate)
            .Take(DefaultPageSize)
            .ToList();
        
        return response;
    }

    public async Task<bool> UpdateWatchedDateAsync(int id, UpdateVideoWatchedDateRequest request, CancellationToken ct)
    {
        var video = await videoRepository.GetAsync(id, ct);
        if (video is null) return false;
         
        video.WatchedDate = request.WatchedDate;
        await videoRepository.UpdateAsync(video, ct);
        return true;
    }
    
    private static VideoResponse MapToVideoResponse(Video video)
    {
        return new VideoResponse
        {
            Id = video.Id,
            YoutubeVideoId = video.YoutubeVideoId,
            Title = video.Title,
            ThumbnailUrl = video.ThumbnailUrl,
            PublishedDate = video.PublishedDate,
            AddedDate = video.AddedDate,
            MetadataLastRefreshedAt = video.MetadataLastRefreshedAt,
            WatchedDate = video.WatchedDate,
            DurationSeconds = video.DurationSeconds,
            ViewCount = video.ViewCount,
            IsWatched = video.IsWatched,
            
            Channel = new VideoResponse.VideoChannelResponse
            {
                Id = video.Channel.Id,
                YoutubeChannelId = video.Channel.YoutubeChannelId,
                Name = video.Channel.Name,
                ThumbnailUrl = video.Channel.ThumbnailUrl,
            },
            
            Categories = video.Channel.Categories
                .Select(category => new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Color = category.Color
                })
                .ToList()
        };
    } 
}