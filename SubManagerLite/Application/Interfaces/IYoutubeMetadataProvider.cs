using SubManagerLite.Application.Features.Channels.Models;

namespace SubManagerLite.Application.Interfaces;

public interface IYoutubeMetadataProvider
{
    Task<YoutubeChannelInfo> GetChannelInfo(YoutubeChannelRef youtubeChannelRef, CancellationToken ct);
}