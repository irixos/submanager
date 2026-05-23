using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Features.Videos.Interfaces;

public interface IYoutubeVideoIngestService
{
    Task<IReadOnlyCollection<Video>> GetRecentVideosAsync(IReadOnlyCollection<Channel> channels, CancellationToken ct);
}