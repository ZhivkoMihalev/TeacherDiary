namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentActivityEntryDto
{
    public DateOnly Date { get; set; }

    public string Description { get; set; } = default!;

    public int PointsEarned { get; set; }
}
