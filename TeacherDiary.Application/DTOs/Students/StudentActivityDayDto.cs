namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentActivityDayDto
{
    public DateOnly Date { get; set; }

    public int PagesRead { get; set; }

    public int AssignmentsCompleted { get; set; }

    public int PointsEarned { get; set; }
}
