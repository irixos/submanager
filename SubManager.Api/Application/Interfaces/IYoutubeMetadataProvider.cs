using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Videos.Models;

namespace SubManager.Api.Application.Interfaces;

public interface IYoutubeMetadataProvider
{
    Task<YoutubeChannelInfo> GetChannelInfo(YoutubeChannelRef youtubeChannelRef, CancellationToken ct);
    Task<YoutubeVideoInfo> GetVideoInfo(string videoId, CancellationToken ct);
}