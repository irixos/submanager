using System.Linq.Expressions;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Categories.Models;
using SubManager.Api.Application.Features.Channels.Models;

namespace SubManager.Api.Application.Features.Channels;

public static class ChannelMappings
{
    public static readonly Expression<Func<Channel, ChannelResponse>> ToChannelResponse = 
        channel => new ChannelResponse
        {
            Id = channel.Id,
            YoutubeChannelId = channel.YoutubeChannelId,
            Name = channel.Name,
            ThumbnailUrl = channel.ThumbnailUrl,
            AddedDate = channel.AddedDate,
            LastCheckedDate = channel.LastCheckedDate,
            IsActive = channel.IsActive,
            Categories = channel.Categories
                .Select(category => new CategoryResponse
                {
                    Id = category.Id,
                    Name = category.Name,
                    Color = category.Color
                })
                .ToList()
        };
    
    public static ChannelResponse MapToChannelResponse(Channel channel)
    {
        return new ChannelResponse
        {
            Id = channel.Id,
            YoutubeChannelId = channel.YoutubeChannelId,
            Name = channel.Name,
            ThumbnailUrl = channel.ThumbnailUrl,
            AddedDate = channel.AddedDate,
            LastCheckedDate = channel.LastCheckedDate,
            IsActive = channel.IsActive,
            Categories = channel.Categories
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