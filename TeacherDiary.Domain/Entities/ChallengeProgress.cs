namespace TeacherDiary.Domain.Entities;

public class ChallengeProgress : BaseEntity
{
    public Guid ChallengeId { get; set; }

    public Challenge Challenge { get; set; } = default!;

    public Guid StudentProfileId { get; set; }

    public StudentProfile StudentProfile { get; set; } = default!;

    public int CurrentValue { get; set; } = 0;

    public bool Completed { get; set; } = false;

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
