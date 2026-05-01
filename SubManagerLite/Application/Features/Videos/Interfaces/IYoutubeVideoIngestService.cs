using SubManagerLite.Application.Entities;

namespace SubManagerLite.Application.Features.Videos.Interfaces;

public interface IYoutubeVideoIngestService
{
    Task<List<Video>> GetRecentVideosAsync(Channel channel, CancellationToken ct);
}