using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Book;

public sealed class AssignedBookStudentProgressDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public ProgressStatus Status { get; set; }
}
