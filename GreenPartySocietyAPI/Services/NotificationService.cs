using GreenPartySocietyAPI.Data;
using GreenPartySocietyAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GreenPartySocietyAPI.Services;

public interface INotificationService
{
    Task CreateAsync(string type, string title, string message, string? relatedEntityId = null);
    Task<IReadOnlyList<AppNotification>> GetRecentAsync(int take = 20);
}

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext db, ILogger<NotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task CreateAsync(string type, string title, string message, string? relatedEntityId = null)
    {
        var notification = new AppNotification
        {
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Notification created: {Type} - {Title}", type, title);
    }

    public async Task<IReadOnlyList<AppNotification>> GetRecentAsync(int take = 20)
    {
        take = Math.Clamp(take, 1, 50);
        return await _db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .ToListAsync();
    }
}
