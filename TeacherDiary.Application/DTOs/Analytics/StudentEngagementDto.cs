namespace TeacherDiary.Application.DTOs.Analytics;

public sealed class StudentEngagementDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = default!;
    public int DaysActiveLast7 { get; set; }
    public int TotalPoints { get; set; }
    public int CurrentStreak { get; set; }
    public bool ActiveToday { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
