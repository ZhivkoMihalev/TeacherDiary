namespace TeacherDiary.Application.DTOs.Reading;

public sealed class BookCreateRequest
{
    public string Title { get; set; } = default!;

    public string Author { get; set; }

    public int? GradeLevel { get; set; }

    public bool IsGlobal { get; set; } = false;

    public int? TotalPages { get; set; }
}
