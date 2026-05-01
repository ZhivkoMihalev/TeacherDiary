using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>
    /// Returns paginated notifications for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Returns the user's notification history, newest first. Each notification includes:
    /// - message text
    /// - read/unread status
    /// - navigation URL (if applicable)
    /// - creation timestamp
    /// </remarks>
    /// <param name="page">Page number (1-based, default: 1).</param>
    /// <param name="pageSize">Number of notifications per page (default: 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of notifications.</returns>
    /// <response code="200">Returns the notification list.</response>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await notificationService.GetForUserAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns the count of unread notifications for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Used by the notification bell to display the unread badge count.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of unread notifications.</returns>
    /// <response code="200">Returns the unread count.</response>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var count = await notificationService.GetUnreadCountAsync(cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    /// <remarks>
    /// Idempotent — marking an already-read notification has no effect.
    /// </remarks>
    /// <param name="id">ID of the notification to mark as read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="204">Notification marked as read.</response>
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Marks all notifications for the current user as read.
    /// </summary>
    /// <remarks>
    /// Bulk operation — marks every unread notification for the authenticated user as read.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="204">All notifications marked as read.</response>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await notificationService.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }
}
