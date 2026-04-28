using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentReadingDto
{
    public Guid AssignedBookId { get; set; }

    public string BookTitle { get; set; } = default!;

    public int CurrentPage { get; set; }

    public int? TotalPages { get; set; }

    public ProgressStatus Status { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public bool IsExpired { get; set; }
}
