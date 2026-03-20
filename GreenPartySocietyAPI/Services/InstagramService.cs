using System.Text.Json;
using GreenPartySocietyAPI.Models;

namespace GreenPartySocietyAPI.Services;

public sealed class InstagramPostDto
{
    public string Id { get; set; } = "";
    public string Caption { get; set; } = "";
    public string? MediaUrl { get; set; }
    public DateTime Timestamp { get; set; }
}

public interface IInstagramService
{
    Task<IReadOnlyList<InstagramPostDto>> GetRecentPostsAsync();
}

public sealed class InstagramService : IInstagramService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<InstagramService> _logger;

    public InstagramService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<InstagramService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<IReadOnlyList<InstagramPostDto>> GetRecentPostsAsync()
    {
        var accessToken = _config["Instagram:AccessToken"];
        var userId = _config["Instagram:UserId"];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Instagram credentials not configured (Instagram:AccessToken, Instagram:UserId). Skipping Instagram fetch.");
            return Array.Empty<InstagramPostDto>();
        }

        try
        {
            var client = _httpFactory.CreateClient("instagram");
            var url = $"https://graph.instagram.com/{userId}/media?fields=id,caption,media_url,timestamp&access_token={accessToken}&limit=10";
            var response = await client.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            var items = doc.RootElement.GetProperty("data");

            var posts = new List<InstagramPostDto>();
            foreach (var item in items.EnumerateArray())
            {
                posts.Add(new InstagramPostDto
                {
                    Id = item.GetProperty("id").GetString() ?? "",
                    Caption = item.TryGetProperty("caption", out var cap) ? cap.GetString() ?? "" : "",
                    MediaUrl = item.TryGetProperty("media_url", out var mu) ? mu.GetString() : null,
                    Timestamp = item.TryGetProperty("timestamp", out var ts)
                        ? DateTime.Parse(ts.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow
                });
            }
            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Instagram posts");
            return Array.Empty<InstagramPostDto>();
        }
    }
}
