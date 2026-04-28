using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Leaderboard;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IGamificationService
{
    Task AddReadingPointsAsync(Guid studentId, int points, CancellationToken cancellationToken);

    Task AddAssignmentPointsAsync(Guid studentId, int points, CancellationToken cancellationToken);

    Task AddChallengePointsAsync(Guid studentId, int points, CancellationToken cancellationToken);

    Task UpdateStreakAsync(Guid studentId, CancellationToken cancellationToken);

    Task<Result<List<LeaderboardItemDto>>> GetLeaderboardAsync(Guid classId, CancellationToken cancellationToken);
}
