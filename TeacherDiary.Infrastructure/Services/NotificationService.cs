using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Notifications;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class NotificationService(
    AppDbContext db,
    ICurrentUser currentUser,
    INotificationPusher pusher) : INotificationService
{
    public async Task CreateAsync(
        Guid userId,
        NotificationType type,
        string message,
        string? navigationUrl,
        Guid? referenceId,
        CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Message = message,
            NavigationUrl = navigationUrl,
            ReferenceId = referenceId,
            IsRead = false
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync(cancellationToken);

        var dto = ToDto(notification);
        await pusher.PushAsync(userId, dto, cancellationToken);
    }

    public async Task<List<NotificationDto>> GetForUserAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return await db.Notifications
            .Where(n => n.UserId == currentUser.UserId && !n.IsDeleted)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                NavigationUrl = n.NavigationUrl,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        return await db.Notifications
            .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead && !n.IsDeleted, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == currentUser.UserId, cancellationToken);

        if (notification is null) return;

        notification.IsRead = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken)
    {
        await db.Notifications
            .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    private static NotificationDto ToDto(Notification n) => new()
    {
        Id = n.Id,
        Message = n.Message,
        Type = n.Type,
        IsRead = n.IsRead,
        NavigationUrl = n.NavigationUrl,
        CreatedAt = n.CreatedAt
    };
}
