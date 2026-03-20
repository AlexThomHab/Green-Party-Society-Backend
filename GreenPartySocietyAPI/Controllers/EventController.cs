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
    public async Task<ActionResult<IReadOnlyList<EventDto>>> Range([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _service.ListRangeAsync(from, to);
        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Policy = "CommitteeOrAdmin")]
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest request)
    {
        var result = await _service.CreateAsync(request);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    [Authorize(Policy = "CommitteeOrAdmin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<EventDto>> Update(string id, [FromBody] UpdateEventRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = result.Error });

        return Ok(result.Data);
    }

    [Authorize(Policy = "CommitteeOrAdmin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result.Success)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = result.Error });

        return NoContent();
    }

    [Authorize(Policy = "CommitteeOrAdmin")]
    [HttpPost("sync/trigger")]
    public async Task<IActionResult> TriggerSync([FromServices] IEventSyncService syncService)
    {
        _ = Task.Run(async () => { try { await syncService.SyncAsync(); } catch { } });
        return Accepted(new { message = "Sync triggered in background." });
    }

    [Authorize(Policy = "CommitteeOrAdmin")]
    [HttpPost("sync/manual")]
    public async Task<ActionResult<EventDto?>> ManualExtract([FromBody] ManualExtractRequest request, [FromServices] IEventSyncService syncService)
    {
        if (string.IsNullOrWhiteSpace(request.Caption))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Caption is required." });

        var extracted = await syncService.ProcessCaptionAsync(request.Caption);
        if (extracted is null || extracted.StartsAt is null)
            return Ok(new { extracted = false, message = "No event detected in this text." });

        var createRequest = new CreateEventRequest
        {
            Title = extracted.Title,
            Description = extracted.Description,
            StartsAt = extracted.StartsAt.Value,
            EndsAt = extracted.EndsAt,
            Location = extracted.Location,
            Source = "instagram_ai"
        };

        var result = await _service.CreateAsync(createRequest);
        if (!result.Success)
            return BadRequest(new ProblemDetails { Title = "Error", Detail = result.Error });

        return Ok(result.Data);
    }
}

public sealed class ManualExtractRequest { public string? Caption { get; set; } }
