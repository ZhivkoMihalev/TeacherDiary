using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class ReadingProgress : BaseEntity
{
    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public Guid AssignedBookId { get; set; }

    public AssignedBook AssignedBook { get; set; } = default!;

    public int CurrentPage { get; set; } = 0;

    public int? TotalPages { get; set; }

    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}
