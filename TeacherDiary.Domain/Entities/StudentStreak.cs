namespace TeacherDiary.Domain.Entities;

public class StudentStreak : BaseEntity
{
    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public int CurrentStreak { get; set; }

    public int BestStreak { get; set; }

    public DateOnly LastActiveDate { get; set; }
}
