using System.Xml.Linq;
using SubManagerLite.Application.Entities;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Interfaces;

namespace SubManagerLite.Application.Features.Videos.Services;

public sealed class YoutubeVideoIngestService (IYoutubeMetadataProvider youtubeMetadataProvider) : IYoutubeVideoIngestService
{
    public async Task<List<Video>> GetRecentVideosAsync(Channel channel, CancellationToken ct)
    {
        var recentVideos = new List<Video>();
        
        var feedUrl = new Uri($"https://www.youtube.com/feeds/videos.xml?channel_id={channel.YoutubeChannelId}");
        
        var doc = XDocument.Load(feedUrl.ToString());
        
        XNamespace yt = "http://www.youtube.com/xml/schemas/2015";
        XNamespace media = "http://search.yahoo.com/mrss/";
        XNamespace atom = "http://www.w3.org/2005/Atom";

        var videos = doc.Root!
            .Elements(atom + "entry");

        foreach (var video in videos)
        {
            var mediaGroup = video.Element(media + "group");
            var mediaCommunity = mediaGroup?.Element(media + "community");
            
            var videoId = video.Element(yt + "videoId")?.Value;
            var title = video.Element(atom + "title")?.Value;
            var publishedDate = video.Element(atom + "published")?.Value;
            var thumbnailUrl = mediaGroup?.Element(media + "thumbnail")?.Attribute("url")?.Value;
            var viewCount = mediaCommunity?.Element(media + "statistics")?.Attribute("views")?.Value;
            
            if (videoId is null || title is null || publishedDate is null)
            {
                var missing = new List<string>();
                
                if (videoId is null) missing.Add("videoId");
                if (title is null) missing.Add("title");
                if (publishedDate is null) missing.Add("publishedDate");
                
                Console.WriteLine($"Missing required fields for video: {string.Join(", ", missing)}");
                
                continue;
            }
            
            var videoInfo = await youtubeMetadataProvider.GetVideoInfo(videoId, ct);
            
            recentVideos.Add(new Video
            {
                YoutubeVideoId = videoId,
                ChannelId = channel.Id,
                Title = title,
                ThumbnailUrl = thumbnailUrl,
                PublishedDate = DateTimeOffset.Parse(publishedDate),
                MetadataLastRefreshedAt = DateTimeOffset.UtcNow,
                DurationSeconds = videoInfo.DurationSeconds,
                ViewCount = long.TryParse(viewCount, out var views) ? views : null,
            });
        }

        return recentVideos;

    }
}