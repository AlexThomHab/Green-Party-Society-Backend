namespace GreenPartySocietyAPI.Models;
public sealed class AppNotification
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "";         // e.g. "new_event"
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? RelatedEntityId { get; set; }
}
