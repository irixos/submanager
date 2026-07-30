using System.Text;
using Microsoft.AspNetCore.Http;
using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Channels.Utilities;
using Xunit;

namespace SubManager.Api.Tests;

public sealed class YoutubeChannelRefParserTests
{
    [Theory]
    [InlineData(
        "youtube.com/@dotnet/videos",
        YoutubeChannelRefKind.Handle,
        "https://youtube.com/@dotnet")]
    [InlineData(
        " https://www.youtube.com/c/Fireship/ ",
        YoutubeChannelRefKind.Custom,
        "https://www.youtube.com/c/Fireship")]
    [InlineData(
        "\"https://m.youtube.com/user/GoogleDevelopers\"",
        YoutubeChannelRefKind.Username,
        "https://m.youtube.com/user/GoogleDevelopers")]
    [InlineData(
        "https://youtube.com/channel/UC_x5XG1OV2P6uZZ5FSM9Ttw",
        YoutubeChannelRefKind.Id,
        "https://youtube.com/channel/UC_x5XG1OV2P6uZZ5FSM9Ttw")]
    public void Parse_SupportedForms_ReturnsCanonicalReference(
        string input,
        YoutubeChannelRefKind expectedKind,
        string expectedUrl)
    {
        var result = YoutubeChannelRefParser.Parse(input);

        Assert.Equal(expectedKind, result.Kind);
        Assert.Equal(expectedUrl, result.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("https://example.com/@dotnet")]
    [InlineData("not a URL")]
    public void Parse_InvalidInput_ThrowsArgumentException(string? input)
    {
        Assert.Throws<ArgumentException>(() => YoutubeChannelRefParser.Parse(input!));
    }

    [Theory]
    [InlineData("https://notyoutube.com/@Fireship")]
    [InlineData("https://example.com/youtube.com/@Fireship")]
    [InlineData("https://www.youtube.com/channel/UC_x5XG1OV2P6uZZ5FSM9TtwA")]
    public async Task ParseFile_InvalidUrlBoundary_ReturnsNoChannels(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "subscriptions.csv");

        var result = await YoutubeChannelRefParser.ParseFile(file, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseFile_CommaDelimitedRows_ExtractsOnlyChannelUrls()
    {
        const string content = """
                               Fireship,https://www.youtube.com/@Fireship,Technology
                               dotnet,https://youtube.com/@dotnet/videos,https://m.youtube.com/user/GoogleDevelopers
                               """;
        var bytes = Encoding.UTF8.GetBytes(content);
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "subscriptions.csv");

        var result = await YoutubeChannelRefParser.ParseFile(file, CancellationToken.None);

        Assert.Collection(
            result,
            channel =>
            {
                Assert.Equal(YoutubeChannelRefKind.Handle, channel.Kind);
                Assert.Equal("https://www.youtube.com/@Fireship", channel.Url);
            },
            channel =>
            {
                Assert.Equal(YoutubeChannelRefKind.Handle, channel.Kind);
                Assert.Equal("https://youtube.com/@dotnet", channel.Url);
            },
            channel =>
            {
                Assert.Equal(YoutubeChannelRefKind.Username, channel.Kind);
                Assert.Equal("https://m.youtube.com/user/GoogleDevelopers", channel.Url);
            });
    }

    [Fact]
    public async Task ParseFile_NullFile_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            YoutubeChannelRefParser.ParseFile(null!, CancellationToken.None));
    }
}
