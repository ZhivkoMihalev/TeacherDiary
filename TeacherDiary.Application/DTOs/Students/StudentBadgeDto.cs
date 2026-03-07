namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentBadgeDto
{
    public string Code { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string Icon { get; set; } = default!;

    public DateTime AwardedAt { get; set; }
}
