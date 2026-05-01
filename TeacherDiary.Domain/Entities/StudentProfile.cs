namespace TeacherDiary.Domain.Entities;

public class StudentProfile : BaseEntity
{
    public Guid? ClassId { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public Guid? ParentId { get; set; }

    public Guid? UserId { get; set; }

    public string AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ReadingProgress> ReadingProgress { get; set; } = new List<ReadingProgress>();

    public ICollection<AssignmentProgress> AssignmentProgress { get; set; } = new List<AssignmentProgress>();

    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public ICollection<ChallengeProgress> ChallengeProgress { get; set; } = new List<ChallengeProgress>();

    public ICollection<StudentBadge> Badges { get; set; } = new List<StudentBadge>();

    public StudentPoints Points { get; set; }

    public StudentStreak Streak { get; set; }

    public int CompletedActivitiesCount { get; set; }

    public ICollection<StudentLearningActivityProgress> LearningActivityProgress { get; set; }
        = new List<StudentLearningActivityProgress>();
}