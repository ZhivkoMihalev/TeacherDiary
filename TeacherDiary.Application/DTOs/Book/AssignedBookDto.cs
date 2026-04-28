namespace TeacherDiary.Application.DTOs.Book;

public sealed class AssignedBookDto
{
    public Guid Id { get; set; }

    public Guid BookId { get; set; }

    public string Title { get; set; } = default!;

    public string Author { get; set; } = default!;

    public int TotalPages { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public int NotStartedCount { get; set; }

    public int InProgressCount { get; set; }

    public int CompletedCount { get; set; }

    public int Points { get; set; }

    public bool IsExpired { get; set; }
}
