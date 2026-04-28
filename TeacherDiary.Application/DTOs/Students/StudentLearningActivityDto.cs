using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentLearningActivityDto
{
    public Guid LearningActivityId { get; set; }

    public string Title { get; set; } = default!;

    public LearningActivityType Type { get; set; }

    public ProgressStatus Status { get; set; }

    public int CurrentValue { get; set; }

    public int? TargetValue { get; set; }

    public int? Score { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public bool IsExpired { get; set; }
}
