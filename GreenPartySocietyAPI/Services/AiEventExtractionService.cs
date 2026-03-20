using System.Text;
using System.Text.Json;

namespace GreenPartySocietyAPI.Services;

public sealed class AiExtractedEvent
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string Location { get; set; } = "";
}

public interface IAiEventExtractionService
{
    Task<AiExtractedEvent?> ExtractEventAsync(string caption, string? imageUrl = null);
}

public sealed class AiEventExtractionService : IAiEventExtractionService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<AiEventExtractionService> _logger;

    public AiEventExtractionService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<AiEventExtractionService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<AiExtractedEvent?> ExtractEventAsync(string caption, string? imageUrl = null)
    {
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Anthropic:ApiKey not configured. Cannot extract event from Instagram post.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(caption))
            return null;

        try
        {
            var client = _httpFactory.CreateClient("anthropic");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var prompt = $$"""
                You are an assistant that extracts event information from Instagram posts for a university Green Party Society.

                Instagram caption:
                "{{caption}}"

                Extract event details if this post describes an event. If it does not describe an event, respond with null.

                Respond ONLY with a JSON object (no markdown, no explanation) in this exact format:
                {
                  "isEvent": true,
                  "title": "Event title",
                  "description": "Brief description",
                  "startsAt": "2026-03-15T18:00:00",
                  "endsAt": "2026-03-15T20:00:00",
                  "location": "Location name"
                }

                Or if not an event:
                {"isEvent": false}

                Use ISO 8601 format for dates. If the year is not mentioned, assume the current year ({{DateTime.UtcNow.Year}}). If time is not mentioned, use null for endsAt. If location is not mentioned, use empty string.
                """;

            var requestBody = new
            {
                model = "claude-haiku-4-5-20251001",
                max_tokens = 500,
                messages = new[] { new { role = "user", content = prompt } }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Anthropic API error: {Status} {Error}", response.StatusCode, error);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var textContent = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "";

            using var resultDoc = JsonDocument.Parse(textContent.Trim());
            var root = resultDoc.RootElement;

            if (!root.TryGetProperty("isEvent", out var isEventProp) || !isEventProp.GetBoolean())
                return null;

            return new AiExtractedEvent
            {
                Title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                Description = root.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "",
                StartsAt = root.TryGetProperty("startsAt", out var s) && s.ValueKind != JsonValueKind.Null
                    ? DateTime.Parse(s.GetString() ?? "") : null,
                EndsAt = root.TryGetProperty("endsAt", out var e) && e.ValueKind != JsonValueKind.Null
                    ? DateTime.Parse(e.GetString() ?? "") : null,
                Location = root.TryGetProperty("location", out var l) ? l.GetString() ?? "" : ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI event extraction failed for caption: {Caption}", caption[..Math.Min(100, caption.Length)]);
            return null;
        }
    }
}
