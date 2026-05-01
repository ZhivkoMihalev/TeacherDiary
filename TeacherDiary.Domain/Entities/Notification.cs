using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public string? NavigationUrl { get; set; }
    public Guid? ReferenceId { get; set; }
}
