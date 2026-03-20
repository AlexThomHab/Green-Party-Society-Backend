using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GreenPartySocietyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET /api/users/me — returns own full profile
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe()
    {
        var jwt = Request.Headers["Authorization"].ToString();
        var result = await _userService.GetMyProfileAsync(jwt);
        if (!result.Success) return Unauthorized();
        return Ok(result.Data);
    }

    // GET /api/users/{id} — returns public profile
    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfileDto>> GetById(string id)
    {
        var result = await _userService.GetProfileAsync(id);
        if (!result.Success) return NotFound(new ProblemDetails { Title = "Not Found", Detail = result.Error });
        return Ok(result.Data);
    }

    // PUT /api/users/me/bio — update own bio
    [Authorize]
    [HttpPut("me/bio")]
    public async Task<ActionResult<UserProfileDto>> UpdateBio([FromBody] UpdateBioRequest request)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await _userService.UpdateBioAsync(userId, request.Bio ?? "");
        if (!result.Success) return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });
        return Ok(result.Data);
    }

    // PUT /api/users/me/substack — link own Substack account
    [Authorize]
    [HttpPut("me/substack")]
    public async Task<ActionResult<UserProfileDto>> UpdateSubstack([FromBody] UpdateSubstackRequest request)
    {
        var userId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var result = await _userService.UpdateSubstackUrlAsync(userId, request.SubstackUrl ?? "");
        if (!result.Success) return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });
        return Ok(result.Data);
    }

    // PUT /api/users/{id}/role — assign role (admin only)
    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id}/role")]
    public async Task<ActionResult<UserProfileDto>> AssignRole(string id, [FromBody] AssignRoleRequest request)
    {
        var requesterId = User.FindFirst("id")?.Value;
        if (string.IsNullOrEmpty(requesterId)) return Unauthorized();
        var result = await _userService.AssignRoleAsync(requesterId, id, request.Role ?? "");
        if (!result.Success) return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });
        return Ok(result.Data);
    }

    // GET /api/users — list all users (admin only)
    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetAll()
    {
        var result = await _userService.GetAllProfilesAsync();
        return Ok(result.Data);
    }
}

public sealed class UpdateBioRequest { public string? Bio { get; set; } }
public sealed class UpdateSubstackRequest { public string? SubstackUrl { get; set; } }
public sealed class AssignRoleRequest { public string? Role { get; set; } }
