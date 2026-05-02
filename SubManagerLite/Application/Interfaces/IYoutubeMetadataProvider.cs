using SubManagerLite.Application.Features.Channels.Models;
using SubManagerLite.Application.Features.Videos.Models;

namespace SubManagerLite.Application.Interfaces;

public interface IYoutubeMetadataProvider
{
    Task<YoutubeChannelInfo> GetChannelInfo(YoutubeChannelRef youtubeChannelRef, CancellationToken ct);
    Task<Dictionary<string, YoutubeVideoInfo>> GetVideoInfo(IReadOnlyCollection<string> videoIds, CancellationToken ct);
}