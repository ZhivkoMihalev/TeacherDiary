using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class StudentLearningActivityProgress : BaseEntity
{
    public Guid LearningActivityId { get; set; }

    public LearningActivity LearningActivity { get; set; } = default!;

    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;

    public int CurrentValue { get; set; } = 0;

    public int? TargetValue { get; set; }

    public int? Score { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public int? ProgressPercent { get; set; }
}
