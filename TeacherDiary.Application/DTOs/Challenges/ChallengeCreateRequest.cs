using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Application.DTOs.Challenges;

public sealed class ChallengeCreateRequest
{
    public string Title { get; set; } = default!;

    public string Description { get; set; }

    public TargetType TargetType { get; set; }

    public int TargetValue { get; set; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddDays(30);

    public int Points { get; set; }
}
