using System.Net;
using System.Net.Http.Json;
using SubManager.Api.Application.Features.Categories.Models;
using SubManager.Api.Tests.Infrastructure;
using Xunit;

namespace SubManager.Api.Tests.Web;

[Trait("Category", "Integration")]
public sealed class CategoryEndpointsIntegrationTests
{
    [Fact]
    public async Task CategoryCrud_AuthenticatedWithAntiforgery_UsesExpectedHttpContracts()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedAntiforgeryClientAsync();

        var createdResponse = await client.PostAsJsonAsync(
            "/categories/",
            new CreateCategoryRequest { Name = "Technology", Color = "#123456" });
        var created = await createdResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal($"/categories/{created.Id}", createdResponse.Headers.Location?.ToString());

        var fetched = await client.GetFromJsonAsync<CategoryResponse>(
            $"/categories/{created.Id}");
        Assert.Equal("Technology", fetched?.Name);
        Assert.Equal("#123456", fetched?.Color);

        var updatedResponse = await client.PutAsJsonAsync(
            $"/categories/{created.Id}",
            new UpdateCategoryRequest { Name = "Updated", ClearColor = true });
        Assert.Equal(HttpStatusCode.NoContent, updatedResponse.StatusCode);

        var updated = await client.GetFromJsonAsync<CategoryResponse>(
            $"/categories/{created.Id}");
        Assert.Equal("Updated", updated?.Name);
        Assert.Null(updated?.Color);

        var deletedResponse = await client.DeleteAsync($"/categories/{created.Id}");
        var missingResponse = await client.GetAsync($"/categories/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deletedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task CategoryRequests_InvalidAndMissing_ReturnBadRequestAndNotFound()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = await factory.CreateAuthenticatedAntiforgeryClientAsync();

        var invalid = await client.PostAsJsonAsync(
            "/categories/",
            new CreateCategoryRequest { Name = " ", Color = "not-a-color" });
        var missingGet = await client.GetAsync("/categories/999");
        var missingUpdate = await client.PutAsJsonAsync(
            "/categories/999",
            new UpdateCategoryRequest { Name = "Missing" });
        var missingDelete = await client.DeleteAsync("/categories/999");

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingGet.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingDelete.StatusCode);
    }

}
