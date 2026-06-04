using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SubManager.Client;
using MudBlazor.Services;
using SubManager.ApiClient;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7208") });

builder.Services.AddScoped<ChannelsClient>();
builder.Services.AddScoped<VideosClient>();
builder.Services.AddScoped<CategoriesClient>();
builder.Services.AddScoped<IdentityClient>();

await builder.Build().RunAsync();
