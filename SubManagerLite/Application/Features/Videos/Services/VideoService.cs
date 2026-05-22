using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Models;
using SubManagerLite.Infrastructure;

namespace SubManagerLite.Application.Features.Videos.Services;

public sealed class VideoService(ApplicationDbContext db) : IVideoService
{
   public async Task<List<VideoResponse>> GetAllAsync(CancellationToken ct)
    {
        return await db.Videos
            .AsNoTracking()
            .Select(VideoMappings.ToVideoResponse)
            .ToListAsync(ct);
    }

    public async Task<VideoResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Videos
            .AsNoTracking()
            .Select(VideoMappings.ToVideoResponse)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<bool> UpdateWatchedDateAsync(int id, UpdateVideoWatchedDateRequest request, CancellationToken ct)
    {
        var video = await db.Videos.FindAsync([id], ct);
        if (video is null) return false;
         
        video.WatchedDate = request.WatchedDate;
        
        db.Videos.Update(video);
        await db.SaveChangesAsync(ct);
        
        return true;
    }
}