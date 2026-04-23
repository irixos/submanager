using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Models;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Models;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Application.Features.Channels.Services;

public sealed class ChannelService(
    IChannelRepository channelRepository,
    IYoutubeChannelRefService youtubeChannelRefService,
    IYoutubeMetadataProvider youtubeMetadataProvider) : IChannelService
{
    public async Task<List<ChannelResponse>> GetAllAsync(CancellationToken ct)
    {
        var channels = await channelRepository.GetAllAsync(ct);
        
        var response = channels.Select(channel => new ChannelResponse
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
            })
            .ToList();
        
        return response;
    }
    
    public async Task<ChannelResponse?> CreateAsync(CreateChannelRequest request, CancellationToken ct)
    {
        var youtubeChannelRef = youtubeChannelRefService.GetYoutubeChannelRef(request.ChannelUrl);
        var channelInfo = await youtubeMetadataProvider.GetChannelInfo(youtubeChannelRef, ct);

        var channel = new Channel
        {
            YoutubeChannelId = channelInfo.YoutubeChannelId,
            Name = channelInfo.Name,
            ThumbnailUrl = channelInfo.ThumbnailUrl,
        };

        try
        {
            await channelRepository.AddAsync(channel, ct);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        var response = new ChannelResponse
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
        
        return response;
    }

    public async Task<bool> UpdateCategoriesAsync(int id, UpdateChannelCategoriesRequest request, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateChannelStatusRequest request, CancellationToken ct)
    {
        var channel = await channelRepository.GetAsync(id, ct);
        if (channel == null) return false;
         
        channel.IsActive = request.IsActive;
        await channelRepository.UpdateAsync(channel, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var channel = await channelRepository.GetAsync(id, ct);
        if (channel == null) return false;
         
        await channelRepository.DeleteAsync(channel, ct);
        return true;
    }
}