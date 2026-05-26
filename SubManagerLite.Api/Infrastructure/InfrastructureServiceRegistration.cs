using SubManagerLite.Application.Interfaces;
using SubManagerLite.Infrastructure.BackgroundServices;
using SubManagerLite.Infrastructure.Integrations;
using YoutubeExplode;

namespace SubManagerLite.Infrastructure;

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