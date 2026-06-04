namespace TeacherDiary.Application.DTOs.Students;

public sealed record GamificationSummaryDto(
    int TotalPoints,
    int CurrentStreak,
    int BestStreak,
    int ClassRank,
    int ClassSize,
    RecentBadgeDto? RecentBadge,
    int TotalBadges);

public sealed record RecentBadgeDto(
    string Code,
    string Name,
    string Icon,
    DateTime AwardedAt);
