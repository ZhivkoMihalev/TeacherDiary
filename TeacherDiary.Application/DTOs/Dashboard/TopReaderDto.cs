namespace TeacherDiary.Application.DTOs.Dashboard;

public sealed class TopReaderDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public int PagesReadLast7Days { get; set; }
}
