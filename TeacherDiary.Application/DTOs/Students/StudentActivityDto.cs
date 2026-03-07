namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentActivityDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public int PagesReadToday { get; set; }

    public int AssignmentsCompletedToday { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public bool IsActiveToday { get; set; }
}
