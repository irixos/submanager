using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubManager.Api.Application.Features.Channels.Models;
using SubManager.Api.Application.Features.Videos.Models;
using SubManager.Api.Application.Interfaces;
using SubManager.Api.Infrastructure;

namespace SubManager.Api.Tests.Infrastructure;

internal sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestAuthenticationScheme = "Test";
    private const string TestUserHeader = "X-Test-User";
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public ApiWebApplicationFactory()
    {
        connection.Open();
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public async Task<HttpClient> CreateAuthenticatedAntiforgeryClientAsync()
    {
        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Add(TestUserHeader, "integration-test-user");
        var token = await client.GetFromJsonAsync<string>("/antiforgery/token");
        client.DefaultRequestHeaders.Add(
            "X-XSRF-TOKEN",
            token ?? throw new InvalidOperationException("Antiforgery token was not returned."));
        return client;
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateHttpsClient();
        client.DefaultRequestHeaders.Add(TestUserHeader, "integration-test-user");
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options
                    .UseSqlite(connection)
                    .ReplaceService<IModelCustomizer, SqliteTestModelCustomizer>());

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationScheme,
                    _ => { });

            services.RemoveAll<IYoutubeMetadataProvider>();
            services.AddSingleton<IYoutubeMetadataProvider, StubYoutubeMetadataProvider>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(TestUserHeader))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "integration-test-user")],
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(
                AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
        }
    }

    private sealed class StubYoutubeMetadataProvider : IYoutubeMetadataProvider
    {
        public Task<YoutubeChannelInfo> GetChannelInfo(
            YoutubeChannelRef youtubeChannelRef,
            CancellationToken ct)
        {
            var isImport = youtubeChannelRef.Url.Contains(
                "@imported",
                StringComparison.OrdinalIgnoreCase);
            return Task.FromResult(new YoutubeChannelInfo
            {
                YoutubeChannelId = isImport ? "UC-imported-channel" : "UC-test-channel",
                Name = isImport ? "Imported channel" : "Test channel",
                ThumbnailUrl = "https://example.com/thumbnail.jpg"
            });
        }

        public Task<YoutubeVideoInfo> GetVideoInfo(string videoId, CancellationToken ct) =>
            throw new NotSupportedException();
    }
}
