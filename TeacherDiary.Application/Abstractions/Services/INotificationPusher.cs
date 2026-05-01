using TeacherDiary.Application.DTOs.Notifications;

namespace TeacherDiary.Application.Abstractions.Services;

public interface INotificationPusher
{
    Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken);
}
