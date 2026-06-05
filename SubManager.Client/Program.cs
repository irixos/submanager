using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SubManager.Client;
using MudBlazor.Services;
using SubManager.ApiClient;
using SubManager.Client.Http;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddTransient<CookieCredentialsHandler>();
builder.Services.AddHttpClient("Api", client =>
    {
        client.BaseAddress = new Uri("https://localhost:7208");
    })
    .AddHttpMessageHandler<CookieCredentialsHandler>();

builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

builder.Services.AddScoped<ChannelsClient>();
builder.Services.AddScoped<VideosClient>();
builder.Services.AddScoped<CategoriesClient>();
builder.Services.AddScoped<IdentityClient>();

await builder.Build().RunAsync();
