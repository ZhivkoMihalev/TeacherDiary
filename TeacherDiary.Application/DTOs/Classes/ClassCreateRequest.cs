namespace TeacherDiary.Application.DTOs.Classes;

public sealed class ClassCreateRequest
{
    public string Name { get; set; } = default!;

    public int Grade { get; set; }

    public string SchoolYear { get; set; } = default!;
}