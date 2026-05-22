using SubManagerLite.Application.Features.Videos.Models;

namespace SubManagerLite.Application.Features.Videos.Interfaces;

public interface IVideoService
{
    Task<List<VideoResponse>> GetAllAsync(CancellationToken ct);
    Task<VideoResponse?> GetAsync(int id, CancellationToken ct);
    Task<bool> UpdateWatchedDateAsync(int id, UpdateVideoWatchedDateRequest request, CancellationToken ct);
}