using Microsoft.EntityFrameworkCore;
using SubManagerLite.Application.Features.Categories.Interfaces;
using SubManagerLite.Application.Features.Categories.Services;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Services;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Services;
using SubManagerLite.Application.Interfaces;
using SubManagerLite.Infrastructure;
using SubManagerLite.Infrastructure.Integrations;
using SubManagerLite.Infrastructure.Repositories;
using SubManagerLite.Web;
using YoutubeExplode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SubManagerLite")));

builder.Services.AddValidation();

builder.Services.AddHttpClient();

// TODO: Move this out later
// Infrastructure
builder.Services.AddSingleton<YoutubeClient>();
builder.Services.AddScoped<IYoutubeMetadataProvider, YoutubeMetadataProvider>();
builder.Services.AddScoped<IChannelRepository, ChannelRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();

// Application Core
builder.Services.AddScoped<IYoutubeChannelRefService, YoutubeChannelRefService>();
builder.Services.AddScoped<IYoutubeVideoIngestService, YoutubeVideoIngestService>();
builder.Services.AddScoped<IChannelService, ChannelService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IVideoService, VideoService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseHttpsRedirection();

// Endpoints
app.MapGroup("/channels")
    .MapChannelsApi();

app.MapGroup("/categories")
    .MapCategoriesApi();

app.MapGroup("/videos")
    .MapVideosApi();

app.Run();