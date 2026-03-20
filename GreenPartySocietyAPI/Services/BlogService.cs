using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using GreenPartySocietyAPI.Models;
using Microsoft.Extensions.Caching.Memory;

namespace GreenPartySocietyAPI.Services;

public interface IBlogService
{
    Task<IReadOnlyList<BlogPostDto>> GetPostsAsync();
    Task<BlogPostDto?> GetBySlugAsync(string slug);
}

public sealed class BlogService : IBlogService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IUserService _userService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BlogService> _logger;
    private const string CacheKey = "blog_posts_all";

    public BlogService(
        IHttpClientFactory httpFactory,
        IUserService userService,
        IMemoryCache cache,
        ILogger<BlogService> logger)
    {
        _httpFactory = httpFactory;
        _userService = userService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BlogPostDto>> GetPostsAsync()
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<BlogPostDto>? cached) && cached is not null)
            return cached;

        var posts = await FetchAllUserPostsAsync();
        _cache.Set(CacheKey, posts, TimeSpan.FromMinutes(10));
        return posts;
    }

    public async Task<BlogPostDto?> GetBySlugAsync(string slug)
    {
        var posts = await GetPostsAsync();
        return posts.FirstOrDefault(p => p.Slug == slug);
    }

    private async Task<IReadOnlyList<BlogPostDto>> FetchAllUserPostsAsync()
    {
        var users = await _userService.GetUsersWithSubstackAsync();

        if (!users.Any())
        {
            _logger.LogInformation("No users have linked a Substack. Blog feed will be empty.");
            return Array.Empty<BlogPostDto>();
        }

        // Fetch all feeds in parallel
        var tasks = users.Select(u => FetchUserFeedAsync(u.SubstackUrl, $"{u.FirstName} {u.LastName}".Trim()));
        var results = await Task.WhenAll(tasks);

        var allPosts = results
            .SelectMany(posts => posts)
            .OrderByDescending(p => p.PublishedAt)
            .Take(50)
            .ToList();

        return allPosts;
    }

    private async Task<IReadOnlyList<BlogPostDto>> FetchUserFeedAsync(string substackUrl, string fallbackAuthor)
    {
        // Normalise: strip trailing slash, append /feed
        var baseUrl = substackUrl.TrimEnd('/');
        var feedUrl = $"{baseUrl}/feed";

        // Extract the Substack username to namespace slugs (e.g. "alexsmith" from "https://alexsmith.substack.com")
        var usernameMatch = Regex.Match(baseUrl, @"https?://([^.]+)\.substack\.com", RegexOptions.IgnoreCase);
        var substackUsername = usernameMatch.Success ? usernameMatch.Groups[1].Value : Guid.NewGuid().ToString("N")[..8];

        try
        {
            var client = _httpFactory.CreateClient("substack");
            client.Timeout = TimeSpan.FromSeconds(10);
            var xml = await client.GetStringAsync(feedUrl);
            using var reader = XmlReader.Create(new StringReader(xml));
            var feed = SyndicationFeed.Load(reader);

            var posts = new List<BlogPostDto>();
            foreach (var item in feed.Items.Take(10))
            {
                var postSlug = ExtractPostSlug(item.Links.FirstOrDefault()?.Uri?.ToString() ?? "");
                // Namespace slug with Substack username to avoid collisions across users
                var combinedSlug = string.IsNullOrEmpty(postSlug)
                    ? $"{substackUsername}--{item.Id}"
                    : $"{substackUsername}--{postSlug}";

                var author = item.Authors.FirstOrDefault()?.Name
                    ?? feed.Authors.FirstOrDefault()?.Name
                    ?? fallbackAuthor;

                string? fullContent = null;
                if (item.ElementExtensions != null)
                {
                    foreach (var ext in item.ElementExtensions)
                    {
                        if (ext.OuterName == "encoded")
                        {
                            fullContent = ext.GetObject<System.Xml.Linq.XElement>()?.Value;
                            break;
                        }
                    }
                }

                var summary = item.Summary?.Text ?? "";
                var previewText = Regex.Replace(summary, "<[^>]+>", "").Trim();
                if (previewText.Length > 300) previewText = previewText[..300] + "\u2026";

                posts.Add(new BlogPostDto
                {
                    Slug = combinedSlug,
                    Title = item.Title?.Text ?? "Untitled",
                    Author = author,
                    PublishedAt = item.PublishDate.UtcDateTime,
                    PreviewText = previewText,
                    Url = item.Links.FirstOrDefault()?.Uri?.ToString() ?? "",
                    Content = fullContent ?? summary
                });
            }

            return posts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Substack feed from {Url}", feedUrl);
            return Array.Empty<BlogPostDto>();
        }
    }

    private static string ExtractPostSlug(string url)
    {
        if (string.IsNullOrEmpty(url)) return "";
        var match = Regex.Match(url, @"/p/([^/?#]+)");
        return match.Success ? match.Groups[1].Value : "";
    }
}
