namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentDetailsDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public bool IsActive { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public int TotalPagesRead { get; set; }

    public int CompletedAssignments { get; set; }

    public List<StudentReadingDto> Reading { get; set; } = new();

    public List<StudentAssignmentDto> Assignments { get; set; } = new();

    public List<StudentActivityDayDto> ActivityLast7Days { get; set; } = new();

    public List<StudentLearningActivityDto> LearningActivities { get; set; } = new();
}
