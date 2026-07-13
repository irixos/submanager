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
