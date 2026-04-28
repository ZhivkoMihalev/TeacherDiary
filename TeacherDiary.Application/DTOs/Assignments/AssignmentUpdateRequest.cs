namespace TeacherDiary.Application.DTOs.Assignments;

public sealed class AssignmentUpdateRequest
{
    public string Title { get; set; } = default!;

    public string Description { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public DateTime? DueDate { get; set; }

    public int Points { get; set; }
}
