namespace TeacherDiary.Application.DTOs.Dashboard;

public sealed class RecentBadgeDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public string BadgeCode { get; set; } = default!;

    public string BadgeName { get; set; } = default!;

    public string BadgeIcon { get; set; } = default!;

    public DateTime AwardedAt { get; set; }
}
