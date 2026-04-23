using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Services;
using SubManagerLite.Application.Interfaces;
using SubManagerLite.Infrastructure;
using SubManagerLite.Infrastructure.Integrations;
using YoutubeExplode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SubManagerLite")));

// TODO: Move this out later
// Infrastructure
builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddScoped<IYoutubeMetadataProvider, YoutubeMetadataProvider>();
// Application Core
builder.Services.AddScoped<IYoutubeChannelRefService, YoutubeChannelRefService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseHttpsRedirection();

app.Run();