using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class AssignmentProgress : BaseEntity
{
    public Guid AssignmentId { get; set; }

    public Assignment Assignment { get; set; } = default!;

    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public int? Score { get; set; }
}
