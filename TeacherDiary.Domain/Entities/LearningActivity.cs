using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class LearningActivity : BaseEntity
{
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = default!;

    public Guid CreatedByTeacherId { get; set; }

    public LearningActivityType Type { get; set; }

    public string Title { get; set; } = default!;

    public string? Description { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? DueDateUtc { get; set; }

    public LearningActivityStatus Status { get; set; }

    // reference към source entity
    public Guid? AssignedBookId { get; set; }

    public Guid? AssignmentId { get; set; }

    public Guid? ChallengeId { get; set; }

    // допълнителни обобщени полета за analytics/UI
    // напр. pages / points / task count
    public int? TargetValue { get; set; }

    public int? MaxScore { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<StudentLearningActivityProgress> StudentProgress { get; set; }
        = new List<StudentLearningActivityProgress>();
}