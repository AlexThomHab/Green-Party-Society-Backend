using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace StemLab.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService, IJwtService jwtService)
    {
        _userService = userService;
    }
   
    [HttpGet("{id}")]
    public async Task<ActionResult<GetUserDetailsResponse>> GetUserById(string id)
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(authHeader))
            return Unauthorized("Missing Authorization header");

        var meResult = await _userService.GetUserDetailsAsync(authHeader);

        if (!meResult.Success || meResult.Data is null)
            return BadRequest(meResult.Error);

        var me = meResult.Data;
        var targetResult = await _userService.GetUserByIdAsync(id);

        if (!targetResult.Success || targetResult.Data is null)
            return BadRequest(targetResult.Error);

        var target = targetResult.Data;

        if (me.Id == target.Id)
        {
            return Ok(target);
        }

        return Ok(new
        {
            id = target.Id,
            target.Email,
            target.FirstName,
            target.LastName
        });
    }
}