namespace GreenPartySocietyAPI.Services;

public sealed class EventSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<EventSyncHostedService> _logger;

    public EventSyncHostedService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<EventSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 30 seconds on startup before first run
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("EventSyncHostedService running at: {Time}", DateTimeOffset.Now);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IEventSyncService>();
                await syncService.SyncAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled event sync");
            }

            var intervalHours = _config.GetValue<int>("EventSync:IntervalHours", 1);
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }
}
