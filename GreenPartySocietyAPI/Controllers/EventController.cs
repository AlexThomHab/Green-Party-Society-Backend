using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GreenPartySocietyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EventsController : ControllerBase
{
    private readonly IEventService _service;

    public EventsController(IEventService service) => _service = service;

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetById(string id)
    {
        var result = await _service.GetByIdAsync(id);
        if (!result.Success)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = result.Error });

        return Ok(result.Data);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> Upcoming([FromQuery] int take = 20)
    {
        var result = await _service.ListUpcomingAsync(take);
        return Ok(result.Data);
    }
    [HttpGet("range")]
    public async Task<ActionResult<IReadOnlyList<EventDto>>> Range([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
    {
        var result = await _service.ListRangeAsync(from, to);
        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest request)
    {
        var result = await _service.CreateAsync(request);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<EventDto>> Update(string id, [FromBody] UpdateEventRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });

        return Ok(result.Data);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result.Success)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = result.Error });

        return NoContent();
    }
}
