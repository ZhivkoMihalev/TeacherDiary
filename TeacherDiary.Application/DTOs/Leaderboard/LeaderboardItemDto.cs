namespace TeacherDiary.Application.DTOs.Leaderboard;

public sealed class LeaderboardItemDto
{
    public Guid StudentId { get; set; }

    public string StudentName { get; set; } = default!;

    public int Points { get; set; }
}
