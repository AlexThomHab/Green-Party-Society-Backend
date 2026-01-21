using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GreenPartySocietyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public AuthController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            var password = request.Password ?? string.Empty;

            var result = await _userService.AuthenticateAsync(email, password);

            if (!result.Success)
                return Unauthorized(new ProblemDetails { Title = "Unauthorized", Detail = "Invalid email or password." });


            var user = result.Data!;
            var token = _jwtService.Generate(
                user.Id,
                user.Email,
                $"{user.FirstName} {user.LastName}"
            );


            return Ok(new TokenResponse { Token = token });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails { Title = "Server Error", Detail = ex.Message });
        }
    }


    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = new User(
                request.FirstName,
                request.LastName,
                request.Email,
                request.Password
            );

            var result = await _userService.AddUserAsync(user);

            if (!result.Success)
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = result.Error
                });

            var created = result.Data!;

            return CreatedAtAction(nameof(Register), new RegisterResponse
            {
                UserId = created.Id,
                Email = created.Email,
                FirstName = created.FirstName,
                LastName = created.LastName
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProblemDetails
            {
                Title = "Server Error",
                Detail = ex.Message
            });
        }
    }

    public sealed class RegisterResponse
    {
        public string UserId { get; set; } = "";
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }


    public sealed class RegisterRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

}