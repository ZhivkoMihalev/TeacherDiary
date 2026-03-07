namespace TeacherDiary.Application.DTOs.Reading;

public sealed class UpdateReadingProgressRequest
{
    public int CurrentPage { get; set; }

    public int? TotalPages { get; set; }

    public bool MarkCompleted { get; set; } = false;
}
