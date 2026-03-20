using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Repositories;

namespace GreenPartySocietyAPI.Services;

public interface IEventService
{
    Task<ServiceResult<EventDto>> GetByIdAsync(string id);
    Task<ServiceResult<IReadOnlyList<EventDto>>> ListUpcomingAsync(int take);
    Task<ServiceResult<IReadOnlyList<EventDto>>> ListRangeAsync(DateTime from, DateTime to);

    Task<ServiceResult<EventDto>> CreateAsync(CreateEventRequest request);
    Task<ServiceResult<EventDto>> UpdateAsync(string id, UpdateEventRequest request);
    Task<ServiceResult<bool>> DeleteAsync(string id);
    Task<ServiceResult<EventDto>> CreateFromExternalAsync(string title, string description, DateTime startsAt, DateTime? endsAt, string location, string source, string externalId);
}

public sealed class EventService : IEventService
{
    private readonly IEventRepository _repo;

    public EventService(IEventRepository repo) => _repo = repo;

    public async Task<ServiceResult<EventDto>> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult<EventDto>.BadRequest("Invalid id");

        var ev = await _repo.GetByIdAsync(id);
        if (ev is null)
            return ServiceResult<EventDto>.BadRequest("Event not found");

        return ServiceResult<EventDto>.Ok(ToDto(ev));
    }

    public async Task<ServiceResult<IReadOnlyList<EventDto>>> ListUpcomingAsync(int take)
    {
        take = Math.Clamp(take, 1, 100);
        var items = await _repo.ListUpcomingAsync(DateTime.UtcNow, take);
        return ServiceResult<IReadOnlyList<EventDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<IReadOnlyList<EventDto>>> ListRangeAsync(DateTime from, DateTime to)
    {
        if (to < from)
            return ServiceResult<IReadOnlyList<EventDto>>.BadRequest("'to' must be >= 'from'");

        var items = await _repo.ListRangeAsync(from, to);
        return ServiceResult<IReadOnlyList<EventDto>>.Ok(items.Select(ToDto).ToList());
    }

    public async Task<ServiceResult<EventDto>> CreateAsync(CreateEventRequest request)
    {
        var validation = Validate(request.Title, request.Description, request.StartsAt, request.EndsAt, request.Location);
        if (validation is not null)
            return ServiceResult<EventDto>.BadRequest(validation);

        var ev = new Event
        {
            Title = request.Title.Trim(),
            Description = (request.Description ?? "").Trim(),
            StartsAtUtc = request.StartsAt,
            EndsAtUtc = request.EndsAt,
            Location = (request.Location ?? "").Trim(),
            Source = request.Source ?? "manual",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(ev);
        return ServiceResult<EventDto>.Ok(ToDto(created));
    }

    public async Task<ServiceResult<EventDto>> UpdateAsync(string id, UpdateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult<EventDto>.BadRequest("Invalid id");

        var ev = await _repo.GetTrackedByIdAsync(id);
        if (ev is null)
            return ServiceResult<EventDto>.BadRequest("Event not found");

        var validation = Validate(request.Title, request.Description, request.StartsAt, request.EndsAt, request.Location);
        if (validation is not null)
            return ServiceResult<EventDto>.BadRequest(validation);

        ev.Title = request.Title.Trim();
        ev.Description = (request.Description ?? "").Trim();
        ev.StartsAtUtc = request.StartsAt;
        ev.EndsAtUtc = request.EndsAt;
        ev.Location = (request.Location ?? "").Trim();
        ev.UpdatedAtUtc = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(ev);
        return ServiceResult<EventDto>.Ok(ToDto(updated));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return ServiceResult<bool>.BadRequest("Invalid id");

        var ok = await _repo.DeleteAsync(id);
        if (!ok)
            return ServiceResult<bool>.BadRequest("Event not found");

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<ServiceResult<EventDto>> CreateFromExternalAsync(string title, string description, DateTime startsAt, DateTime? endsAt, string location, string source, string externalId)
    {
        if (await _repo.ExistsByExternalIdAsync(externalId))
            return ServiceResult<EventDto>.BadRequest("Duplicate event");

        var ev = new Event
        {
            Title = title.Trim(),
            Description = description.Trim(),
            StartsAtUtc = startsAt,
            EndsAtUtc = endsAt,
            Location = location.Trim(),
            Source = source,
            ExternalId = externalId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var created = await _repo.AddAsync(ev);
        return ServiceResult<EventDto>.Ok(ToDto(created));
    }

    private static string? Validate(string title, string? description, DateTime startsAt, DateTime? endsAt, string? location)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required.";

        if (title.Trim().Length > 200)
            return "Title must be 200 characters or fewer.";

        if ((description ?? "").Length > 4000)
            return "Description must be 4000 characters or fewer.";

        if ((location ?? "").Length > 300)
            return "Location must be 300 characters or fewer.";

        if (endsAt is not null && endsAt < startsAt)
            return "EndsAt must be after StartsAt.";

        return null;
    }

    private static EventDto ToDto(Event e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        StartsAt = e.StartsAtUtc,
        EndsAt = e.EndsAtUtc,
        Location = e.Location,
        Source = e.Source
    };
}

public sealed class EventDto
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string Location { get; set; } = "";
    public string Source { get; set; } = "manual";
}

public sealed class CreateEventRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
    public string Source { get; set; } = "manual";
}

public sealed class UpdateEventRequest
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Location { get; set; }
}
