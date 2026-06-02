namespace TeacherDiary.Application.DTOs.Analytics;

public sealed class SubjectCompletionDto
{
    public string Subject { get; set; } = default!;
    public int TotalRows { get; set; }
    public int CompletedRows { get; set; }
    public double CompletionRate { get; set; }
}
