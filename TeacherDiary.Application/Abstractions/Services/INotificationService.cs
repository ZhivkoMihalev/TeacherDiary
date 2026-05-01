using TeacherDiary.Application.DTOs.Notifications;
using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.Abstractions.Services;

public interface INotificationService
{
    Task CreateAsync(Guid userId, NotificationType type, string message, string? navigationUrl, Guid? referenceId, CancellationToken cancellationToken);
    Task<List<NotificationDto>> GetForUserAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<int> GetUnreadCountAsync(CancellationToken cancellationToken);
    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken);
    Task MarkAllAsReadAsync(CancellationToken cancellationToken);
}
