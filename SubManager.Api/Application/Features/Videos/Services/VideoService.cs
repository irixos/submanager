using Gridify;
using Gridify.EntityFramework;
using Microsoft.EntityFrameworkCore;
using SubManager.Api.Application.Features.Videos.Interfaces;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.BackgroundServices;

namespace SubManager.Api.Application.Features.Videos.Services;

public sealed class VideoService(
    ApplicationDbContext db,
    IMetadataTaskQueue metadataTaskQueue) : IVideoService
{
    public async Task<Paging<VideoResponse>> GetAllAsync(GridifyQuery query, CancellationToken ct)
    {
        return await db.Videos
            .Select(VideoMappings.ToVideoResponse)
            .GridifyAsync(query.ClampPageSize(), ct);
    }

    public async Task<VideoResponse?> GetAsync(int id, CancellationToken ct)
    {
        return await db.Videos
            .Select(VideoMappings.ToVideoResponse)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<VideoDurationStatusResponse> GetDurationStatusAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct)
    {
        var durations = ids.Count == 0
            ? []
            : await db.Videos
                .Where(video => ids.Contains(video.Id))
                .Select(video => new VideoDurationResponse
                {
                    Id = video.Id,
                    DurationSeconds = video.DurationSeconds
                })
                .ToListAsync(ct);

        return new VideoDurationStatusResponse
        {
            HasPendingMetadata = metadataTaskQueue.HasPendingWork,
            Videos = durations
        };
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
