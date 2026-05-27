using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Application.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace SubManager.Api.Infrastructure.Integrations;

public sealed class YoutubeMetadataProvider(YoutubeClient youtubeClient) : IYoutubeMetadataProvider
{
    public async Task<YoutubeChannelInfo> GetChannelInfo(YoutubeChannelRef youtubeChannelRef, CancellationToken ct)
    {
        var channel = youtubeChannelRef.Kind switch
        {
            YoutubeChannelRefKind.Id =>
                await youtubeClient.Channels.GetAsync(youtubeChannelRef.Url, ct),

            YoutubeChannelRefKind.Handle =>
                await youtubeClient.Channels.GetByHandleAsync(youtubeChannelRef.Url, ct),

            YoutubeChannelRefKind.Custom =>
                await youtubeClient.Channels.GetBySlugAsync(youtubeChannelRef.Url, ct),

            YoutubeChannelRefKind.Username =>
                await youtubeClient.Channels.GetByUserAsync(youtubeChannelRef.Url, ct),

            _ => throw new ArgumentOutOfRangeException(
                nameof(youtubeChannelRef),
                youtubeChannelRef.Kind,
                "Invalid channel reference kind.")
        };
        
        return new YoutubeChannelInfo
        {
            YoutubeChannelId = channel.Id,
            Name = channel.Title,
            ThumbnailUrl = channel.Thumbnails.GetWithHighestResolution()?.Url
        };
    }

    public async Task<YoutubeVideoInfo> GetVideoInfo(string videoId, CancellationToken ct)
    {

        var video = await youtubeClient.Videos.GetAsync(videoId, ct);
        var duration = (int)(video.Duration?.TotalSeconds ?? 0);

        return new YoutubeVideoInfo(duration);
    }
}