using SubManagerLite.Application.Features.Categories.Interfaces;
using SubManagerLite.Application.Features.Categories.Services;
using SubManagerLite.Application.Features.Channels.Interfaces;
using SubManagerLite.Application.Features.Channels.Services;
using SubManagerLite.Application.Features.Videos.Interfaces;
using SubManagerLite.Application.Features.Videos.Services;

namespace SubManagerLite.Application;

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