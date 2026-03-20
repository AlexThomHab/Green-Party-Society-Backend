namespace GreenPartySocietyAPI.Models;
public sealed class BlogPostDto
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime PublishedAt { get; set; }
    public string PreviewText { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Content { get; set; }
}
