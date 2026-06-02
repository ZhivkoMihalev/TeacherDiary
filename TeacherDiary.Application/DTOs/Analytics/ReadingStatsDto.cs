namespace TeacherDiary.Application.DTOs.Analytics;

public sealed class ReadingStatsDto
{
    public int NotStartedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int PagesReadLast7Days { get; set; }
    public int PagesReadLast30Days { get; set; }
}
