namespace TeacherDiary.Application.DTOs.Reading;

public sealed class AssignBookRequest
{
    public Guid BookId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
