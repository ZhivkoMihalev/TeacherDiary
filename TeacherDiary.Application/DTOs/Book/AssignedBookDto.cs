namespace TeacherDiary.Application.DTOs.Book;

public sealed class AssignedBookDto
{
    public Guid AssignedBookId { get; set; }

    public Guid BookId { get; set; }

    public string Title { get; set; } = default!;

    public string Author { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int StudentsReading { get; set; }

    public int StudentsCompleted { get; set; }
}
