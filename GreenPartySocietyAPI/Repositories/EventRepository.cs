using GreenPartySocietyAPI.Data;
using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenPartySocietyAPI.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(string id);
    Task<Event?> GetTrackedByIdAsync(string id);

    Task<IReadOnlyList<Event>> ListUpcomingAsync(DateTimeOffset fromInclusive, int take);
    Task<IReadOnlyList<Event>> ListRangeAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive);

    Task<Event> AddAsync(Event ev);
    Task<Event> UpdateAsync(Event ev);
    Task<bool> DeleteAsync(string id);
}

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;

    public EventRepository(AppDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(string id)
        => _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);

    public Task<Event?> GetTrackedByIdAsync(string id)
        => _db.Events.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<IReadOnlyList<Event>> ListUpcomingAsync(DateTimeOffset fromInclusive, int take)
        => await _db.Events.AsNoTracking()
            .Where(e => e.StartsAt >= fromInclusive)
            .OrderBy(e => e.StartsAt)
            .Take(take)
            .ToListAsync();

    public async Task<IReadOnlyList<Event>> ListRangeAsync(DateTimeOffset fromInclusive, DateTimeOffset toInclusive)
        => await _db.Events.AsNoTracking()
            .Where(e => e.StartsAt >= fromInclusive && e.StartsAt <= toInclusive)
            .OrderBy(e => e.StartsAt)
            .ToListAsync();

    public async Task<Event> AddAsync(Event ev)
    {
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return ev;
    }

    public async Task<Event> UpdateAsync(Event ev)
    {
        _db.Events.Update(ev);
        await _db.SaveChangesAsync();
        return ev;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var existing = await _db.Events.FirstOrDefaultAsync(e => e.Id == id);
        if (existing is null) return false;

        _db.Events.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }
}
