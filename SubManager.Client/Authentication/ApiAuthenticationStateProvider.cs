using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SubManager.ApiClient;

namespace SubManager.Client.Authentication;

public sealed class ApiAuthenticationStateProvider(IdentityClient identityClient) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var info = await identityClient.InfoGETAsync();
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, info.Email), new Claim(ClaimTypes.Email, info.Email)],
                "Identity.Application");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (ApiException)
        {
            return Anonymous;
        }
        catch (HttpRequestException)
        {
            return Anonymous;
        }
    }

    public async Task RefreshAsync()
    {
        var authenticationState = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
    }
}
