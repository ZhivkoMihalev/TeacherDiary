using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Assignments;

public sealed class AssignmentStudentProgressDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = default!;
    public ProgressStatus Status { get; set; }
}
