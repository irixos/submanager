using System.Collections.Concurrent;
using SubManagerLite.Application.Features.Channels;
using SubManagerLite.Application.Features.Channels.Models;
using SubManagerLite.Application.Features.Videos.Models;
using SubManagerLite.Application.Interfaces;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace SubManagerLite.Infrastructure.Integrations;

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

    public async Task<Dictionary<string, YoutubeVideoInfo>> GetVideoInfo(IReadOnlyCollection<string> videoIds, CancellationToken ct)
    {
        var videoInfos = new ConcurrentDictionary<string, YoutubeVideoInfo>();
        
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 10,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(videoIds, parallelOptions, async (videoId, loopCt) =>
        {
            var video = await youtubeClient.Videos.GetAsync(videoId, loopCt);
            var duration = (int)(video.Duration?.TotalSeconds ?? 0);

            videoInfos[videoId] = new YoutubeVideoInfo(duration);
        });

        return videoInfos.ToDictionary();
    }
}