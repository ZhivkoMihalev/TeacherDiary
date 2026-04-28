namespace TeacherDiary.Application.DTOs.Reading;

public sealed class AssignBookRequest
{
    public Guid BookId { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public int Points { get; set; }
}
