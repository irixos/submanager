using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace SubManager.Client.Http;

public sealed class CookieCredentialsHandler(NavigationManager navigationManager) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        if (request.Method == HttpMethod.Post &&
            request.RequestUri?.AbsolutePath == "/channels/import")
        {
            var tokenUri = new Uri(request.RequestUri, "/antiforgery/token");
            using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, tokenUri);
            tokenRequest.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            using var tokenResponse = await base.SendAsync(tokenRequest, cancellationToken);
            tokenResponse.EnsureSuccessStatusCode();
            var token = await tokenResponse.Content.ReadFromJsonAsync<string>(cancellationToken)
                        ?? throw new InvalidOperationException("The antiforgery token response was empty.");
            request.Headers.Add("X-XSRF-TOKEN", token);
        }

        var returnUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
            !IsExpectedUnauthorizedRequest(request.RequestUri))
        {
            var loginUrl = $"{navigationManager.BaseUri}login?returnUrl={Uri.EscapeDataString(returnUrl)}";
            navigationManager.NavigateTo(loginUrl, replace: true);
        }

        return response;
    }

    private static bool IsExpectedUnauthorizedRequest(Uri? requestUri)
    {
        var path = requestUri?.AbsolutePath;
        return path is "/register" or "/login" or "/refresh" or "/confirmEmail" or
                   "/resendConfirmationEmail" or "/forgotPassword" or "/resetPassword" ||
               path?.StartsWith("/manage/", StringComparison.OrdinalIgnoreCase) == true ||
               path?.StartsWith("/identity/", StringComparison.OrdinalIgnoreCase) == true;
    }
}
