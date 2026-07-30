namespace SubManager.Api.Infrastructure.BackgroundServices;

public sealed class MetadataHostedService(
    IMetadataTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<MetadataHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("MetadataHostedService started.");

        await BackgroundProcessing(ct);
    }
    
    private async Task BackgroundProcessing(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var workItem = await taskQueue.DequeueAsync(ct);

            using var scope = serviceScopeFactory.CreateScope();
            
            try { await workItem(scope.ServiceProvider, ct); }
            catch (Exception ex) { logger.LogError(ex, $"Error executing {nameof(workItem)}."); }
            finally { taskQueue.MarkCompleted(); }
        }
    }
    
    public override async Task StopAsync(CancellationToken ct)
    {
        logger.LogInformation("MetadataHostedService is stopping.");

        await base.StopAsync(ct);
    }
}
