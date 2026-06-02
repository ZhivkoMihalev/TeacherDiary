namespace TeacherDiary.Application.DTOs.Analytics;

public sealed class DailyActivityDto
{
    public DateOnly Date { get; set; }
    public int ActiveStudents { get; set; }
    public int PagesRead { get; set; }
    public int PointsEarned { get; set; }
    public int CompletedAssignments { get; set; }
}
