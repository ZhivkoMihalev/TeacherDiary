using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public ActivityType ActivityType { get; set; }

    public ActivityReferenceType ReferenceType { get; set; }

    public Guid ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public int? PagesRead { get; set; }

    public int? PointsEarned { get; set; }
}
