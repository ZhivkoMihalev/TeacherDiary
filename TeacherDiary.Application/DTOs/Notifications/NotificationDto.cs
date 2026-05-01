using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Notifications;

public sealed class NotificationDto
{
    public Guid Id { get; set; }
    public string Message { get; set; } = default!;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public string? NavigationUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
