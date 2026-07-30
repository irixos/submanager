using Gridify;
using SubManager.Api.Application.Features.Videos.Models;

namespace SubManager.Api.Application.Features.Videos.Interfaces;

public interface IVideoService
{
    Task<Paging<VideoResponse>> GetAllAsync(GridifyQuery query, CancellationToken ct);
    Task<VideoResponse?> GetAsync(int id, CancellationToken ct);
    Task<VideoDurationStatusResponse> GetDurationStatusAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken ct);
    Task<bool> UpdateWatchedDateAsync(int id, UpdateVideoWatchedDateRequest request, CancellationToken ct);
}
