using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Domain.Entities;

public class Challenge : BaseEntity
{
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string Description { get; set; }

    public string? TargetDescription { get; set; }

    public TargetType TargetType { get; set; }

    public int TargetValue { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime EndDate { get; set; }

    public int Points { get; set; }

    public ICollection<ChallengeProgress> Progress { get; set; } = new List<ChallengeProgress>();
}
