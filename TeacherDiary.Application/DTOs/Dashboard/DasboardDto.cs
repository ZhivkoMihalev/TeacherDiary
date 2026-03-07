using TeacherDiary.Application.DTOs.Leaderboard;
using TeacherDiary.Application.DTOs.Students;

namespace TeacherDiary.Application.DTOs.Dashboard;

public sealed class DashboardDto
{
    public Guid ClassId { get; set; }

    public string ClassName { get; set; } = default!;

    public int StudentsCount { get; set; }

    public int ActiveTodayCount { get; set; }

    public int InactiveTodayCount { get; set; }

    public int TotalPagesReadLast7Days { get; set; }

    public int CompletedAssignmentsLast7Days { get; set; }

    public int ActiveLearningActivitiesCount { get; set; }

    public int CompletedLearningActivitiesLast7Days { get; set; }

    public List<LeaderboardItemDto> Leaderboard { get; set; } = new();

    public List<TopReaderDto> TopReaders { get; set; } = new();

    public List<StudentStreakDto> BestStreaks { get; set; } = new();

    public List<RecentBadgeDto> RecentBadges { get; set; } = new();
}
