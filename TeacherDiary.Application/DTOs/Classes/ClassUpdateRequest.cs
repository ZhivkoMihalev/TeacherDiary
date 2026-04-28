namespace TeacherDiary.Application.DTOs.Classes;

public sealed class ClassUpdateRequest
{
    public string Name { get; set; } = default!;

    public int Grade { get; set; }

    public string SchoolYear { get; set; } = default!;
}
