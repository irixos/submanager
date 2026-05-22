using SubManagerLite.Application.Features.Videos.Models;

namespace SubManagerLite.Application.Features.Videos.Interfaces;

public interface IVideoRefreshService
{
    Task<RefreshResult> RefreshAllAsync(CancellationToken ct);

    Task RefreshMetadataForVideosAsync(
        IReadOnlyCollection<string> newVideoIds, 
        CancellationToken ct);
}