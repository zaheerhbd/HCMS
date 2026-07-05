using HCM.Application.Notifications.DataHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HCM.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationDataHandler handler) : ControllerBase
{
    private readonly INotificationDataHandler _handler = handler;

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    /// <summary>GET api/notifications — latest 20 notifications for the current user</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(CancellationToken ct)
    {
        var result = await _handler.GetMyNotificationsAsync(CurrentUserId, 20, ct);
        return Ok(result);
    }

    /// <summary>GET api/notifications/unread-count — unread badge count</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await _handler.GetUnreadCountAsync(CurrentUserId, ct);
        return Ok(result);
    }

    /// <summary>POST api/notifications/{id}/read — mark a single notification as read</summary>
    [HttpPost("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        await _handler.MarkReadAsync(CurrentUserId, id, ct);
        return NoContent();
    }

    /// <summary>POST api/notifications/read-all — mark all as read</summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _handler.MarkAllReadAsync(CurrentUserId, ct);
        return NoContent();
    }
}
