namespace TeacherDiary.Application.DTOs.Reading;

public sealed class UpdateAssignedBookRequest
{
    public DateTime StartDateUtc { get; set; }

    public DateTime EndDateUtc { get; set; }

    public int Points { get; set; }
}
