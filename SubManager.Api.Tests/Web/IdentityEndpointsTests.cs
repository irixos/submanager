using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubManager.Api.Application.Entities;
using SubManager.Api.Application.Features.Identity.Models;
using SubManager.Api.Infrastructure;
using SubManager.Api.Infrastructure.Identity;
using SubManager.Api.Tests.Infrastructure;
using SubManager.Api.Web;
using Xunit;

namespace SubManager.Api.Tests.Web;

public sealed class IdentityEndpointsTests
{
    private const string Email = "owner@example.com";
    private const string Password = "Password123!";

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

    [Fact]
    public async Task Register_NewUser_IsEmailConfirmed()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();

        var response = await RegisterAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Users.SingleAsync();
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task ChangeEmail_ValidRequest_UpdatesLoginAndRefreshesCookieIdentity()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);

        var response = await client.PostAsJsonAsync(
            "/identity/change-email",
            new ChangeEmailRequest { NewEmail = "new-owner@example.com", Password = Password });
        var infoResponse = await client.GetAsync("/manage/info");
        using var info = JsonDocument.Parse(await infoResponse.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, infoResponse.StatusCode);
        Assert.Equal("new-owner@example.com", info.RootElement.GetProperty("email").GetString());

        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Users.SingleAsync();
        Assert.Equal("new-owner@example.com", user.Email);
        Assert.Equal("new-owner@example.com", user.UserName);
        Assert.True(user.EmailConfirmed);
    }

    [Fact]
    public async Task ChangeEmail_WrongPassword_PreservesLogin()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);

        var response = await client.PostAsJsonAsync(
            "/identity/change-email",
            new ChangeEmailRequest { NewEmail = "new-owner@example.com", Password = "wrong" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var user = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Users.SingleAsync();
        Assert.Equal(Email, user.Email);
        Assert.Equal(Email, user.UserName);
    }

    [Fact]
    public async Task ChangeEmail_InvalidEmail_ReturnsBadRequest()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);

        var response = await client.PostAsJsonAsync(
            "/identity/change-email",
            new ChangeEmailRequest { NewEmail = "not-an-email", Password = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.Equal(
            Email,
            (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Users.SingleAsync()).Email);
    }

    [Fact]
    public async Task ChangeEmail_DuplicateEmail_ReturnsBadRequest()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var result = await userManager.CreateAsync(new ApplicationUser
            {
                Email = "other@example.com",
                UserName = "other@example.com"
            }, Password);
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        }

        var response = await client.PostAsJsonAsync(
            "/identity/change-email",
            new ChangeEmailRequest { NewEmail = "other@example.com", Password = Password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var verificationScope = factory.Services.CreateAsyncScope();
        var original = await verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Users.SingleAsync(user => user.Email == Email);
        Assert.Equal(Email, original.UserName);
    }

    [Fact]
    public async Task ChangeEmail_AnonymousRequest_ReturnsUnauthorized()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/identity/change-email",
            new ChangeEmailRequest { NewEmail = "new-owner@example.com", Password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_ValidPassword_RemovesAllDataAllowsRegistrationAndRejectsStaleAuthentication()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);
        await SeedApplicationDataAsync(factory);
        var accessToken = await LoginForAccessTokenAsync(client);
        using var staleClient = factory.CreateHttpsClient();
        staleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync(
            "/identity/delete-account",
            new DeleteAccountRequest { Password = Password });
        var staleResponse = await staleClient.GetAsync("/categories");
        var registration = await RegisterAsync(client, "replacement@example.com");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal("replacement@example.com", (await db.Users.SingleAsync()).Email);
        Assert.Empty(await db.Channels.ToListAsync());
        Assert.Empty(await db.Videos.ToListAsync());
        Assert.Empty(await db.Categories.ToListAsync());
    }

    [Fact]
    public async Task DeleteAccount_WrongPassword_PreservesAccountAndApplicationData()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);
        await SeedApplicationDataAsync(factory);

        var response = await client.PostAsJsonAsync(
            "/identity/delete-account",
            new DeleteAccountRequest { Password = "wrong" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(1, await db.Channels.CountAsync());
        Assert.Equal(1, await db.Videos.CountAsync());
        Assert.Equal(1, await db.Categories.CountAsync());
    }

    [Fact]
    public async Task DeleteAccount_AnonymousRequest_ReturnsUnauthorized()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync(
            "/identity/delete-account",
            new DeleteAccountRequest { Password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForgotAndResetPassword_KnownEmail_UsesCapturedCodeAndChangesPassword()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync(client)).StatusCode);

        var forgotResponse = await client.PostAsJsonAsync("/forgotPassword", new { email = Email });
        var resetCode = factory.EmailSender.PasswordResetCode;
        var resetResponse = await client.PostAsJsonAsync("/resetPassword", new
        {
            email = Email,
            resetCode,
            newPassword = "ChangedPassword123!"
        });
        var oldLogin = await LoginAsync(client);
        var newLogin = await LoginAsync(client, password: "ChangedPassword123!");

        Assert.Equal(HttpStatusCode.OK, forgotResponse.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(resetCode));
        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_RejectsPreviousBearerAccessToken()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync(client)).StatusCode);
        var accessToken = await LoginForAccessTokenAsync(client);
        using var staleClient = factory.CreateHttpsClient();
        staleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/forgotPassword", new { email = Email })).StatusCode);
        var resetResponse = await client.PostAsJsonAsync("/resetPassword", new
        {
            email = Email,
            resetCode = factory.EmailSender.PasswordResetCode,
            newPassword = "ChangedPassword123!"
        });
        var staleResponse = await staleClient.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.OK, resetResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
    }

    [Theory]
    [InlineData("/forgotPassword", HttpStatusCode.OK)]
    [InlineData("/resetPassword", HttpStatusCode.BadRequest)]
    public async Task PasswordRecovery_AnonymousRequests_AreRateLimitedAfterFiveAttempts(
        string path,
        HttpStatusCode expectedStatus)
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        var request = path == "/forgotPassword"
            ? new { email = "unknown@example.com", resetCode = "", newPassword = "" }
            : new { email = "unknown@example.com", resetCode = "invalid", newPassword = "ChangedPassword123!" };

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(expectedStatus, (await client.PostAsJsonAsync(path, request)).StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync(path, request)).StatusCode);
    }

    [Theory]
    [InlineData("/forgotPassword", "/FORGOTPASSWORD", HttpStatusCode.OK)]
    [InlineData("/forgotPassword", "/forgotPassword/", HttpStatusCode.OK)]
    [InlineData("/resetPassword", "/RESETPASSWORD", HttpStatusCode.BadRequest)]
    [InlineData("/resetPassword", "/resetPassword/", HttpStatusCode.BadRequest)]
    public async Task PasswordRecovery_RouteVariants_ShareRateLimit(
        string path,
        string caseVariant,
        HttpStatusCode expectedStatus)
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        var request = path == "/forgotPassword"
            ? new { email = "unknown@example.com", resetCode = "", newPassword = "" }
            : new { email = "unknown@example.com", resetCode = "invalid", newPassword = "ChangedPassword123!" };

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(expectedStatus, (await client.PostAsJsonAsync(path, request)).StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync(caseVariant, request)).StatusCode);
    }

    [Fact]
    public async Task PasswordRecovery_Operations_HaveIndependentRateLimits()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        var forgotRequest = new { email = "unknown@example.com" };
        var resetRequest = new
        {
            email = "unknown@example.com",
            resetCode = "invalid",
            newPassword = "ChangedPassword123!"
        };

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/forgotPassword", forgotRequest)).StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/forgotPassword", forgotRequest)).StatusCode);

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/resetPassword", resetRequest)).StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/resetPassword", resetRequest)).StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsGenericSuccess()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync(client)).StatusCode);

        var response = await client.PostAsJsonAsync(
            "/forgotPassword",
            new { email = "unknown@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(factory.EmailSender.PasswordResetCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidCode_ReturnsBadRequest()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync(client)).StatusCode);

        var response = await client.PostAsJsonAsync("/resetPassword", new
        {
            email = Email,
            resetCode = "invalid",
            newPassword = "ChangedPassword123!"
        });
        var originalLogin = await LoginAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, originalLogin.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidNewPassword_ReturnsBadRequest()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync(client)).StatusCode);
        Assert.Equal(
            HttpStatusCode.OK,
            (await client.PostAsJsonAsync("/forgotPassword", new { email = Email })).StatusCode);

        var response = await client.PostAsJsonAsync("/resetPassword", new
        {
            email = Email,
            resetCode = factory.EmailSender.PasswordResetCode,
            newPassword = "x"
        });
        var originalLogin = await LoginAsync(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, originalLogin.StatusCode);
    }

    [Fact]
    public async Task Logout_AuthenticatedSession_EndsSession()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);

        var logoutResponse = await client.PostAsync("/identity/logout", null);
        var infoResponse = await client.GetAsync("/manage/info");

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, infoResponse.StatusCode);
    }

    [Fact]
    public async Task ManageInfo_ChangePassword_UpdatesCredentialsAndEndsSession()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);

        var changeResponse = await client.PostAsJsonAsync("/manage/info", new
        {
            oldPassword = Password,
            newPassword = "ChangedPassword123!"
        });
        var infoResponse = await client.GetAsync("/manage/info");
        var oldLogin = await LoginAsync(client);
        var newLogin = await LoginAsync(client, password: "ChangedPassword123!");

        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, infoResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ManageInfo_ChangePassword_RejectsPreviousBearerAccessToken()
    {
        using var factory = new ApiWebApplicationFactory(useTestAuthentication: false);
        using var client = factory.CreateHttpsClient();
        await RegisterAndLoginAsync(client);
        var accessToken = await LoginForAccessTokenAsync(client);
        using var staleClient = factory.CreateHttpsClient();
        staleClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var changeResponse = await client.PostAsJsonAsync("/manage/info", new
        {
            oldPassword = Password,
            newPassword = "ChangedPassword123!"
        });
        var staleResponse = await staleClient.GetAsync("/categories");

        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, staleResponse.StatusCode);
    }

    private static async Task RegisterAndLoginAsync(HttpClient client)
    {
        Assert.Equal(HttpStatusCode.OK, (await RegisterAsync(client)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(client)).StatusCode);
    }

    private static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string email = Email) =>
        client.PostAsJsonAsync("/register", new { email, password = Password });

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string email = Email,
        string password = Password) =>
        client.PostAsJsonAsync(
            "/login?useCookies=true&useSessionCookies=false",
            new { email, password });

    private static async Task<string> LoginForAccessTokenAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/login", new { email = Email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()
               ?? throw new InvalidOperationException("Login did not return an access token.");
    }

    private static async Task SeedApplicationDataAsync(ApiWebApplicationFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;
        var channel = new Channel
        {
            YoutubeChannelId = "UC-delete-test",
            Name = "Delete test",
            AddedDate = now,
            IsActive = true,
            Categories = [new Category { Name = "Delete category", Color = "#123456" }],
            Videos =
            [
                new Video
                {
                    YoutubeVideoId = "delete-video",
                    Title = "Delete video",
                    PublishedDate = now,
                    AddedDate = now,
                    MetadataLastRefreshedAt = now
                }
            ]
        };
        db.Channels.Add(channel);
        await db.SaveChangesAsync();
    }
}
