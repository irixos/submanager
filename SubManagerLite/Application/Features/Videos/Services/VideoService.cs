using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Models;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Models;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Application.Features.Videos.Services;

public sealed class VideoService(
    IVideoRepository videoRepository,
    IChannelRepository channelRepository,
    IYoutubeVideoIngestService youtubeVideoIngestService) : IVideoService
{
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
        var checkedAt = DateTimeOffset.UtcNow;
        var newVideos = new List<Video>();

        // get new videos for each channel
        foreach (var channel in activeChannels)
        {
            newVideos.AddRange(await youtubeVideoIngestService.GetRecentVideosAsync(channel, ct));
            channel.LastCheckedDate = checkedAt;
        }

        // update channel last checked date
        await channelRepository.SaveChangesAsync(ct);
        
        if (newVideos.Count == 0)
            return [];
        
        //upsert new videos
        await videoRepository.UpsertRangeAsync(newVideos, ct);
        
        // get newly refreshed videos
        var youtubeVideoIds = newVideos
            .Select(v => v.YoutubeVideoId)
            .Distinct()
            .ToList();
        
        var refreshedVideos = await videoRepository.GetByYoutubeVideoIdsAsync(youtubeVideoIds, ct);
        
        // return new feed items
        var response = refreshedVideos.Select(MapToVideoResponse).ToList();
        
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