using GreenPartySocietyAPI.Data;
using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenPartySocietyAPI.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(string id);
    Task<Event?> GetTrackedByIdAsync(string id);

    Task<IReadOnlyList<Event>> ListUpcomingAsync(DateTime fromInclusiveUtc, int take);
    Task<IReadOnlyList<Event>> ListRangeAsync(DateTime fromInclusiveUtc, DateTime toInclusiveUtc);

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

    public async Task<IReadOnlyList<Event>> ListUpcomingAsync(DateTime fromInclusiveUtc, int take)
        => await _db.Events.AsNoTracking()
            .Where(e => e.StartsAtUtc >= fromInclusiveUtc)
            .OrderBy(e => e.StartsAtUtc)
            .Take(take)
            .ToListAsync();

    public async Task<IReadOnlyList<Event>> ListRangeAsync(DateTime fromInclusiveUtc, DateTime toInclusiveUtc)
        => await _db.Events.AsNoTracking()
            .Where(e => e.StartsAtUtc >= fromInclusiveUtc && e.StartsAtUtc <= toInclusiveUtc)
            .OrderBy(e => e.StartsAtUtc)
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
