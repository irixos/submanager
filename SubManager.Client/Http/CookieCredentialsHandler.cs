using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace SubManager.Client.Http;

public class CookieCredentialsHandler : DelegatingHandler
 {
     protected override Task<HttpResponseMessage> SendAsync(
         HttpRequestMessage request,
         CancellationToken cancellationToken)
     {
         request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
         request.Headers.Add("X-Requested-With", "XMLHttpRequest");

         return base.SendAsync(request, cancellationToken);
     }
 }