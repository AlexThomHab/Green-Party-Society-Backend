using GreenPartySocietyAPI.Models;

namespace GreenPartySocietyAPI.Services;

public interface IEventSyncService
{
    Task SyncAsync();
    Task<AiExtractedEvent?> ProcessCaptionAsync(string caption);
}

public sealed class EventSyncService : IEventSyncService
{
    private readonly IInstagramService _instagram;
    private readonly IAiEventExtractionService _ai;
    private readonly IEventService _events;
    private readonly INotificationService _notifications;
    private readonly ILogger<EventSyncService> _logger;

    public EventSyncService(
        IInstagramService instagram,
        IAiEventExtractionService ai,
        IEventService events,
        INotificationService notifications,
        ILogger<EventSyncService> logger)
    {
        _instagram = instagram;
        _ai = ai;
        _events = events;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task SyncAsync()
    {
        _logger.LogInformation("Starting Instagram event sync at {Time}", DateTime.UtcNow);
        int created = 0, skipped = 0, errors = 0;

        IReadOnlyList<InstagramPostDto> posts;
        try { posts = await _instagram.GetRecentPostsAsync(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Instagram posts during sync");
            return;
        }

        foreach (var post in posts)
        {
            try
            {
                var extracted = await _ai.ExtractEventAsync(post.Caption, post.MediaUrl);
                if (extracted is null || extracted.StartsAt is null) { skipped++; continue; }

                var result = await _events.CreateFromExternalAsync(
                    extracted.Title,
                    extracted.Description,
                    extracted.StartsAt.Value,
                    extracted.EndsAt,
                    extracted.Location,
                    "instagram_ai",
                    post.Id);

                if (!result.Success) { skipped++; continue; } // duplicate

                await _notifications.CreateAsync(
                    "new_event",
                    "New Event Detected",
                    $"{extracted.Title} has been added from Instagram.",
                    result.Data!.Id);

                created++;
                _logger.LogInformation("Created event from Instagram post {PostId}: {Title}", post.Id, extracted.Title);
            }
            catch (Exception ex)
            {
                errors++;
                _logger.LogError(ex, "Error processing Instagram post {PostId}", post.Id);
            }
        }

        _logger.LogInformation("Sync complete. Created: {Created}, Skipped: {Skipped}, Errors: {Errors}", created, skipped, errors);
    }

    public Task<AiExtractedEvent?> ProcessCaptionAsync(string caption)
        => _ai.ExtractEventAsync(caption);
}
