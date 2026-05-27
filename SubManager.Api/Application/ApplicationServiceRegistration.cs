using SubManager.Api.Application.Features.Categories.Interfaces;
using SubManager.Api.Application.Features.Categories.Services;
using SubManager.Api.Application.Features.Channels.Interfaces;
using SubManager.Api.Application.Features.Channels.Services;
using SubManager.Api.Application.Features.Videos.Interfaces;
using SubManager.Api.Application.Features.Videos.Services;

namespace SubManager.Api.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IVideoRefreshService, VideoRefreshService>();
        services.AddScoped<IYoutubeVideoIngestService, YoutubeVideoIngestService>();
        
        return services;
    }
}