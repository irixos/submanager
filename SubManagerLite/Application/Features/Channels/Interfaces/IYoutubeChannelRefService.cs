using SubManagerLite.Application.Features.Channels.Models;

namespace SubManagerLite.Application.Features.Channels.Interfaces;

public interface IYoutubeChannelRefService
{
    YoutubeChannelRef GetYoutubeChannelRef(string channelUrl);
}