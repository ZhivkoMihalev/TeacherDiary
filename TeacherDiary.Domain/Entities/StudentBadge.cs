namespace TeacherDiary.Domain.Entities;

public class StudentBadge : BaseEntity
{
    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public Guid BadgeId { get; set; }

    public Badge Badge { get; set; } = default!;

    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}
