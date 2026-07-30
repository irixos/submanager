using System.Net;
using System.Text;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Videos.Services;
using Xunit;

namespace SubManager.Api.Tests.Application.Features.Videos.Services;

public sealed class YoutubeVideoIngestServiceTests
{
    [Fact]
    public async Task GetRecentVideosAsync_MixedFeed_MapsFreshCompleteEntriesAndUpdatesChannel()
    {
        var before = DateTimeOffset.UtcNow;
        var freshDate = before.AddDays(-1);
        var oldDate = before.AddDays(-15);
        var xml = $$"""
                    <feed xmlns="http://www.w3.org/2005/Atom"
                          xmlns:yt="http://www.youtube.com/xml/schemas/2015"
                          xmlns:media="http://search.yahoo.com/mrss/">
                      <entry>
                        <yt:videoId>short-video</yt:videoId>
                        <title>Short video</title>
                        <link href="https://www.youtube.com/shorts/short-video" />
                        <published>{{freshDate:O}}</published>
                        <media:group>
                          <media:thumbnail url="https://image/short" />
                          <media:community><media:statistics views="123" /></media:community>
                        </media:group>
                      </entry>
                      <entry>
                        <yt:videoId>normal-video</yt:videoId>
                        <title>Normal video</title>
                        <link href="https://www.youtube.com/watch?v=normal-video" />
                        <published>{{freshDate.AddHours(1):O}}</published>
                        <media:group>
                          <media:community><media:statistics views="unknown" /></media:community>
                        </media:group>
                      </entry>
                      <entry>
                        <yt:videoId>old-video</yt:videoId>
                        <title>Old video</title>
                        <published>{{oldDate:O}}</published>
                      </entry>
                      <entry>
                        <yt:videoId>missing-title</yt:videoId>
                        <published>{{freshDate:O}}</published>
                      </entry>
                    </feed>
                    """;
        var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(xml, Encoding.UTF8, "application/xml")
            });
        var channel = CreateChannel();
        var service = new YoutubeVideoIngestService(new StubHttpClientFactory(handler));

        var result = await service.GetRecentVideosAsync([channel], CancellationToken.None);

        Assert.Equal(2, result.Count);
        var shortVideo = Assert.Single(result, video => video.YoutubeVideoId == "short-video");
        Assert.Equal(channel.Id, shortVideo.ChannelId);
        Assert.Equal("Short video", shortVideo.Title);
        Assert.Equal("https://image/short", shortVideo.ThumbnailUrl);
        Assert.Equal(123, shortVideo.ViewCount);
        Assert.True(shortVideo.IsShort);
        var normalVideo = Assert.Single(result, video => video.YoutubeVideoId == "normal-video");
        Assert.Null(normalVideo.ViewCount);
        Assert.False(normalVideo.IsShort);
        Assert.NotNull(channel.LastCheckedDate);
        Assert.All(result, video => Assert.Equal(channel.LastCheckedDate, video.MetadataLastRefreshedAt));
        Assert.InRange(channel.LastCheckedDate.Value, before, DateTimeOffset.UtcNow);
        Assert.Equal(
            $"https://www.youtube.com/feeds/videos.xml?channel_id={channel.YoutubeChannelId}",
            Assert.Single(handler.RequestedUris).ToString());
    }

    [Fact]
    public async Task GetRecentVideosAsync_HttpFailure_Propagates()
    {
        var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var service = new YoutubeVideoIngestService(new StubHttpClientFactory(handler));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetRecentVideosAsync([CreateChannel()], CancellationToken.None));
    }

    private static Channel CreateChannel()
    {
        return new Channel
        {
            Id = 42,
            YoutubeChannelId = "UC-feed",
            Name = "Feed",
            AddedDate = DateTimeOffset.UtcNow,
            IsActive = true
        };
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<Uri> RequestedUris { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestedUris.Add(request.RequestUri!);
            return Task.FromResult(responseFactory(request));
        }
    }
}
