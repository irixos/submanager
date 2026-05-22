using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Models;
using SubManagerLite.Application.Interfaces;
using SubManagerLite.Infrastructure;

namespace SubManagerLite.Application.Features.Channels.Services;

public sealed class ChannelService(
    ApplicationDbContext db,
    IYoutubeMetadataProvider youtubeMetadataProvider) : IChannelService
{
    public async Task<List<ChannelResponse>> GetAllAsync(CancellationToken ct)
    {
        return await db.Channels
            .AsNoTracking()
            .Select(ChannelMappings.ToChannelResponse)
            .ToListAsync(ct); 
    }

    public async Task<ChannelResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Channels
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ChannelMappings.ToChannelResponse)
            .FirstOrDefaultAsync(ct);
    }
    
    public async Task<ChannelResponse?> CreateAsync(CreateChannelRequest request, CancellationToken ct)
    {
        var youtubeChannelRef = YoutubeChannelRefParser.Parse(request.ChannelUrl);
        var channelInfo = await youtubeMetadataProvider.GetChannelInfo(youtubeChannelRef, ct);

        var channel = new Channel
        {
            YoutubeChannelId = channelInfo.YoutubeChannelId,
            Name = channelInfo.Name,
            ThumbnailUrl = channelInfo.ThumbnailUrl,
            IsActive = true,
            Categories = await db.Categories
                .Where(c => request.CategoryIds.Contains((c.Id)))
                .ToListAsync(ct)
        };

        try
        {
            await db.Channels.AddAsync(channel, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return null;
        }

        var response = ChannelMappings.MapToChannelResponse(channel);
        
        return response;
    }

    public async Task<bool> UpdateCategoriesAsync(int id, UpdateChannelCategoriesRequest request, CancellationToken ct)
    {
        var channel = await db.Channels.FindAsync([id], ct);
        if (channel is null) return false;

        if (request.CategoryIds is not null)
        {
            var newCategories = await db.Categories
                .Where(c => request.CategoryIds.Contains(c.Id))
                .ToListAsync(ct);
            
            if (newCategories.Count != request.CategoryIds.Distinct().Count())
                throw new ArgumentException("Invalid category ids");
            
            channel.Categories.Clear();
            
            foreach (var category in newCategories)
                channel.Categories.Add(category);
        }
        else 
            channel.Categories.Clear();

        db.Channels.Update(channel);
        await db.SaveChangesAsync(ct);
        
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateChannelStatusRequest request, CancellationToken ct)
    {
        var channel = await db.Channels.FindAsync([id], ct);
        if (channel is null) return false;
         
        channel.IsActive = request.IsActive;
        
        db.Channels.Update(channel);
        await db.SaveChangesAsync(ct);
        
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var channel = await db.Channels.FindAsync([id], ct);
        if (channel is null) return false;
         
        db.Channels.Remove(channel);
        await db.SaveChangesAsync(ct);
        
        return true;
    }
}