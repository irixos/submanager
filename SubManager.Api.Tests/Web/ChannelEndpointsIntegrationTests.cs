using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Web;

[Trait("Category", "Integration")]
public sealed class ChannelEndpointsIntegrationTests
{
    [Fact]
    public async Task ChannelCreate_DuplicateYoutubeId_ReturnsConflict()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedAntiforgeryClientAsync();
        var request = new CreateChannelRequest { ChannelUrl = "youtube.com/@test" };

        var firstResponse = await client.PostAsJsonAsync("/channels/", request);
        var first = await firstResponse.Content.ReadFromJsonAsync<ChannelResponse>();
        var duplicateResponse = await client.PostAsJsonAsync(
            "/channels/",
            new CreateChannelRequest { ChannelUrl = "youtube.com/@another" });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.NotNull(first);
        Assert.Equal("UC-test-channel", first.YoutubeChannelId);
        Assert.Equal("Test channel", first.Name);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task ChannelImport_ValidAndInvalidFiles_ReturnExpectedContracts()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedAntiforgeryClientAsync();

        using var validContent = FileContent(
            "https://youtube.com/@imported",
            "channels.txt");
        var validResponse = await client.PostAsync("/channels/import", validContent);
        var result = await validResponse.Content.ReadFromJsonAsync<ImportChannelsResponse>();

        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(1, result.CandidatesFound);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal("UC-imported-channel", Assert.Single(result.ImportedChannels).YoutubeChannelId);

        using var invalidContent = FileContent(
            "https://youtube.com/@another",
            "channels.xml");
        var invalidResponse = await client.PostAsync("/channels/import", invalidContent);

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task ChannelImport_MissingAntiforgeryToken_ReturnsBadRequest()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateAuthenticatedClient();
        using var content = FileContent(
            "https://youtube.com/@imported",
            "channels.txt");

        var response = await client.PostAsync("/channels/import", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static MultipartFormDataContent FileContent(string content, string fileName)
    {
        var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(file, "File", fileName);
        return multipart;
    }
}
