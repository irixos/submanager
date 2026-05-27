using SubManager.Api.Application.Features.Videos.Models;

namespace SubManager.Api.Application.Features.Videos.Interfaces;

public interface IVideoRefreshService
{
    Task<RefreshResult> RefreshAllAsync(CancellationToken ct);

    Task RefreshMetadataForVideosAsync(IReadOnlyCollection<string> newVideoIds, CancellationToken ct);
}