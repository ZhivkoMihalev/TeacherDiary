namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentStreakDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public int CurrentStreak { get; set; }

    public int BestStreak { get; set; }
}
