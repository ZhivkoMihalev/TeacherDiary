namespace TeacherDiary.Domain.Entities;

public class StudentPoints : BaseEntity
{
    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public int TotalPoints { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
