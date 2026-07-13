using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using SubManager.ApiClient;

namespace SubManager.Client.Authentication;

public sealed class ApiAuthenticationStateProvider(IdentityClient identityClient) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public bool IsUnavailable { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        GetAuthenticationStateAsync(CancellationToken.None);

    private async Task<AuthenticationState> GetAuthenticationStateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var info = await identityClient.InfoGETAsync(cancellationToken);
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, info.Email), new Claim(ClaimTypes.Email, info.Email)],
                "Identity.Application");

            IsUnavailable = false;
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch (ApiException exception)
        {
            IsUnavailable = exception.StatusCode != 401;
            return Anonymous;
        }
        catch (HttpRequestException)
        {
            IsUnavailable = true;
            return Anonymous;
        }
    }

    public async Task<AuthenticationState> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var authenticationState = await GetAuthenticationStateAsync(cancellationToken);
        NotifyAuthenticationStateChanged(Task.FromResult(authenticationState));
        return authenticationState;
    }

    public void MarkAnonymous()
    {
        IsUnavailable = false;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }
}
