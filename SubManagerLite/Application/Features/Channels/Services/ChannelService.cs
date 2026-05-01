using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Categories.Models;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Models;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Application.Features.Channels.Services;

public sealed class ChannelService(
    IChannelRepository channelRepository,
    ICategoryRepository categoryRepository,
    IYoutubeChannelRefService youtubeChannelRefService,
    IYoutubeMetadataProvider youtubeMetadataProvider) : IChannelService
{
    public async Task<List<ChannelResponse>> GetAllAsync(CancellationToken ct)
    {
        var channels = await channelRepository.GetAllAsync(ct);
        
        var response = channels.Select(MapToChannelResponse).ToList();
        
        return response;
    }

    public async Task<ChannelResponse?> GetAsync(int id, CancellationToken ct)
    {
        var channel = await channelRepository.GetAsync(id, ct);
        if (channel is null) return null;

        var response = MapToChannelResponse(channel);

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
            IsActive = true,
        };

        try
        {
            await channelRepository.AddAsync(channel, ct);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        var response = MapToChannelResponse(channel);
        
        return response;
    }

    public async Task<bool> UpdateCategoriesAsync(int id, UpdateChannelCategoriesRequest request, CancellationToken ct)
    {
        var channel = await channelRepository.GetAsync(id, ct);
        if (channel is null) return false;

        if (request.CategoryIds is not null)
        {
            var newCategories = await categoryRepository.GetByIdsAsync(request.CategoryIds, ct);
            
            if (newCategories.Count != request.CategoryIds.Distinct().Count())
                throw new ArgumentException("Invalid category ids");
            
            channel.Categories.Clear();
            
            foreach (var category in newCategories)
                channel.Categories.Add(category);
        }
        else 
            channel.Categories.Clear();

        await channelRepository.UpdateAsync(channel, ct);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateChannelStatusRequest request, CancellationToken ct)
    {
        var channel = await channelRepository.GetAsync(id, ct);
        if (channel is null) return false;
         
        channel.IsActive = request.IsActive;
        await channelRepository.UpdateAsync(channel, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var channel = await channelRepository.GetAsync(id, ct);
        if (channel is null) return false;
         
        await channelRepository.DeleteAsync(channel, ct);
        return true;
    }
    
    private static ChannelResponse MapToChannelResponse(Channel channel)
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