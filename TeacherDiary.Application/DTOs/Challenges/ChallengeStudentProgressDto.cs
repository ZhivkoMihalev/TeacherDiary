namespace TeacherDiary.Application.DTOs.Challenges;

public sealed class ChallengeStudentProgressDto
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = default!;
    public bool Started { get; set; }
    public bool Completed { get; set; }
    public int CurrentValue { get; set; }
}
