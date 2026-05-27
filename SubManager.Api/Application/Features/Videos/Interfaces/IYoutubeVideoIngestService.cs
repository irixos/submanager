using SubManager.Api.Application.Entities;

namespace SubManager.Api.Application.Features.Videos.Interfaces;

public interface IYoutubeVideoIngestService
{
    Task<IReadOnlyCollection<Video>> GetRecentVideosAsync(IReadOnlyCollection<Channel> channels, CancellationToken ct);
}