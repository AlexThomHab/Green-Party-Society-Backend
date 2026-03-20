using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GreenPartySocietyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class BlogController : ControllerBase
{
    private readonly IBlogService _blog;

    public BlogController(IBlogService blog) => _blog = blog;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BlogPostDto>>> GetPosts()
    {
        var posts = await _blog.GetPostsAsync();
        return Ok(posts);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<BlogPostDto>> GetBySlug(string slug)
    {
        var post = await _blog.GetBySlugAsync(slug);
        if (post is null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Post not found." });
        return Ok(post);
    }
}
