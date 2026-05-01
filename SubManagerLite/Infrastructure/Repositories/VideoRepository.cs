using System.Runtime.InteropServices.JavaScript;
using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Infrastructure.Repositories;

public class VideoRepository(ApplicationDbContext db) : IVideoRepository
{
    private const int MaxUpsertBatchSize = 200;
    
    public Task<List<Video>> GetAllAsync(CancellationToken ct)
    {
        return db.Videos
            .Include(v => v.Channel)
            .ThenInclude(c => c.Categories)
            .ToListAsync(ct);
    }

    public Task<Video?> GetAsync(int id, CancellationToken ct)
    {
        return db.Videos
            .Include(v => v.Channel)
            .ThenInclude(c => c.Categories)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public Task<List<Video>> GetByYoutubeVideoIdsAsync(List<string> youtubeVideoIds, CancellationToken ct)
    {
        return db.Videos
            .Include(v => v.Channel)
            .ThenInclude(c => c.Categories)
            .Where(v => youtubeVideoIds.Contains(v.YoutubeVideoId))
            .ToListAsync(ct);       
    }

    public Task AddAsync(Video video, CancellationToken ct)
    {
        db.Videos.Add(video);
        return db.SaveChangesAsync(ct);
    }

    public Task UpdateAsync(Video video, CancellationToken ct)
    {
        db.Videos.Update(video);
        return db.SaveChangesAsync(ct);
    }

    public async Task UpsertRangeAsync(List<Video> videos, CancellationToken ct)
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
}