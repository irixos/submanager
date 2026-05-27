using SubManager.Api.Application.Interfaces;
using SubManager.Api.Infrastructure.BackgroundServices;
using SubManager.Api.Infrastructure.Integrations;
using YoutubeExplode;

namespace SubManager.Api.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddSingleton<YoutubeClient>();
        services.AddSingleton<IMetadataTaskQueue, MetadataTaskQueue>();

        services.AddHostedService<MetadataHostedService>();

        services.AddScoped<IYoutubeMetadataProvider, YoutubeMetadataProvider>();
        
        return services;
    }
}