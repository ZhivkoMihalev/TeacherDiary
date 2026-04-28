namespace TeacherDiary.Application.DTOs.Assignments;

public sealed class AssignmentListDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string Subject { get; set; } = default!;

    public string Description { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    public int TotalStudents { get; set; }

    public int CompletedCount { get; set; }

    public int Points { get; set; }

    public bool IsExpired { get; set; }
}
