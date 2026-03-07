namespace TeacherDiary.Application.DTOs.Classes;

public sealed class ClassDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;

    public int Grade { get; set; }

    public string SchoolYear { get; set; } = default!;

    public int StudentsCount { get; set; }
}
