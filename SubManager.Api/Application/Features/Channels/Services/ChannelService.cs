using Gridify;
using Gridify.EntityFramework;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Channels.Interfaces;
using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Channels.Utilities;
using SubManager.Api.Application.Interfaces;
using SubManager.Api.Infrastructure;

namespace SubManager.Api.Application.Features.Channels.Services;

public sealed class ChannelService(
    ApplicationDbContext db,
    IYoutubeMetadataProvider youtubeMetadataProvider) : IChannelService
{
    public async Task<Paging<ChannelResponse>> GetAllAsync(GridifyQuery query, CancellationToken ct)
    {
        return await db.Channels
            .Select(ChannelMappings.ToChannelResponse)
            .GridifyAsync(query.ClampPageSize(), ct);
    }

    public async Task<ChannelResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Channels
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
    
    public async Task<ImportChannelsResponse?> ImportAsync(ImportChannelsRequest request, CancellationToken ct)
    {
        // get list of refs parsed from file
        var parsedRefs = await YoutubeChannelRefParser.ParseFile(request.File, ct);
        if (parsedRefs.Count == 0) return null;
        
        // dedupe urls in refs
        var candidatesById = parsedRefs
            .GroupBy(YoutubeChannelRefParser.GetChannelId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        
        // get list of channel ids
        var candidateIds = candidatesById.Keys.ToList();

        // get list of channels that already exist in db
        var existingIds = await db.Channels
            .Where(c => candidateIds.Contains(c.YoutubeChannelId))
            .Select(c => c.YoutubeChannelId)
            .ToListAsync(ct);
        
        var existingIdSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        // filter out channels already present in database
        var channelsToImport = candidatesById
            .Where(kvp => !existingIdSet.Contains(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();
        
        // import channels
        var importedChannels = new List<Channel>();
        var failedCount = 0;
        
        foreach (var channelRef in channelsToImport)
        {
            try
            {
                var channelInfo = await youtubeMetadataProvider.GetChannelInfo(channelRef, ct);

                importedChannels.Add(new Channel
                {
                    YoutubeChannelId = channelInfo.YoutubeChannelId,
                    Name = channelInfo.Name,
                    ThumbnailUrl = channelInfo.ThumbnailUrl,
                    IsActive = true,
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                failedCount++;
            }
        }

        await db.Channels.AddRangeAsync(importedChannels, ct);
        await db.SaveChangesAsync(ct);
        
        var importedCount = importedChannels.Count;
        var candidatesFound = candidatesById.Count;
        var duplicateCount = existingIdSet.Count;
        
        return new ImportChannelsResponse
        {
            CandidatesFound = candidatesFound,
            DuplicateCount = duplicateCount,
            ImportedCount = importedCount,
            FailedCount = failedCount,
            ImportedChannels = importedChannels.Select(ChannelMappings.MapToChannelResponse).ToList()
        };
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