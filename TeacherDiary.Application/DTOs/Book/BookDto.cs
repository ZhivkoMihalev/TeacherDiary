namespace TeacherDiary.Application.DTOs.Book;

public sealed class BookDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string Author { get; set; }

    public int? GradeLevel { get; set; }
}
