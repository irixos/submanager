using System.Linq.Expressions;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Categories.Models;
using SubManager.Api.Application.Features.Videos.Models;

namespace SubManager.Api.Application.Features.Videos;

public static class VideoMappings
{
    public static readonly Expression<Func<Video, VideoResponse>> ToVideoResponse = 
        video => new VideoResponse
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
            IsShort = video.IsShort,
            
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