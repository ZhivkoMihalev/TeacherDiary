namespace TeacherDiary.Application.DTOs.Reading;

public sealed class BookUpdateRequest
{
    public string Title { get; set; } = default!;

    public string Author { get; set; } = default!;

    public int GradeLevel { get; set; }

    public int TotalPages { get; set; }
}
