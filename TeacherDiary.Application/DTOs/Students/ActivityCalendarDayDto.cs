namespace TeacherDiary.Application.DTOs.Students;

public sealed class ActivityCalendarDayDto
{
    public DateOnly Date { get; set; }

    public bool HasActivity { get; set; }

    public int PointsEarned { get; set; }

    public int PagesRead { get; set; }

    public int ActivityCount { get; set; }
}
