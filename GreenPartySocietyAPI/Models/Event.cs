namespace GreenPartySocietyAPI.Models;

public sealed class Event
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }

    public string Location { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string Source { get; set; } = "manual";   // "manual" | "instagram_ai"
    public string? ExternalId { get; set; }            // Instagram post ID for dedup
}