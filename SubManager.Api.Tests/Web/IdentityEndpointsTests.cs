using System.Net;
using System.Net.Http.Json;
using SubManager.Api.Tests.Infrastructure;
using SubManager.Api.Web;
using Xunit;

namespace SubManager.Api.Tests.Web;

public sealed class IdentityEndpointsTests
{
    [Fact]
    public async Task SetupStatus_BeforeAndAfterRegistration_ReflectsAvailability()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var before = await client.GetFromJsonAsync<SetupStatusResponse>(
            "/identity/setup-status");
        var registration = await client.PostAsJsonAsync(
            "/register",
            new { email = "owner@example.com", password = "Password123!" });
        var after = await client.GetFromJsonAsync<SetupStatusResponse>(
            "/identity/setup-status");

        Assert.True(before?.CanRegister);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        Assert.False(after?.CanRegister);
    }

    [Fact]
    public async Task Register_SecondUser_ReturnsConflict()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var first = await client.PostAsJsonAsync(
            "/register",
            new { email = "owner@example.com", password = "Password123!" });
        var second = await client.PostAsJsonAsync(
            "/register",
            new { email = "other@example.com", password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ProtectedApi_AnonymousRequest_ReturnsUnauthorized()
    {
        using var factory = new ApiWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var response = await client.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
