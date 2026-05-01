namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentChallengeDto
{
    public Guid ChallengeId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? TargetDescription { get; set; }
    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }
    public bool Started { get; set; }
    public bool Completed { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsExpired { get; set; }
}