namespace TeacherDiary.Application.DTOs.Analytics;

public sealed class ClassAnalyticsDto
{
    public List<DailyActivityDto> ActivityTimeline { get; set; } = new();
    public List<StudentEngagementDto> StudentEngagement { get; set; } = new();
    public List<SubjectCompletionDto> SubjectCompletion { get; set; } = new();
    public ReadingStatsDto ReadingStats { get; set; } = new();
}
