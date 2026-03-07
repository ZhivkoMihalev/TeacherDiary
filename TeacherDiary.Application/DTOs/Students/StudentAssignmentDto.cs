using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentAssignmentDto
{
    public string Title { get; set; } = default!;

    public string Subject { get; set; } = default!;

    public ProgressStatus Status { get; set; }

    public DateTime? DueDate { get; set; }
}
