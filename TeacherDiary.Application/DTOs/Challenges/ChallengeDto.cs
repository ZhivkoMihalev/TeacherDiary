using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Challenges;

public sealed class ChallengeDto
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public TargetType TargetType { get; set; }

    public int TargetValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int TotalStudents { get; set; }

    public int CompletedCount { get; set; }

    public int Points { get; set; }

    public bool IsExpired { get; set; }
}
