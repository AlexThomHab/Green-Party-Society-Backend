using GreenPartySocietyAPI.Models;
using GreenPartySocietyAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GreenPartySocietyAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationController(INotificationService notifications) => _notifications = notifications;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppNotification>>> GetRecent([FromQuery] int take = 20)
    {
        var items = await _notifications.GetRecentAsync(take);
        return Ok(items);
    }
}
