using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Leaderboard;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class GamificationService(AppDbContext db, ICurrentUser currentUser) : IGamificationService
{
    public async Task AddReadingPointsAsync(Guid studentId, int points, CancellationToken cancellationToken)
    {
        if (points <= 0) return;
        await AddPointsAsync(studentId, points, cancellationToken);
    }

    public async Task AddAssignmentPointsAsync(Guid studentId, int points, CancellationToken cancellationToken)
    {
        if (points <= 0) return;
        await AddPointsAsync(studentId, points, cancellationToken);
    }

    public async Task AddChallengePointsAsync(Guid studentId, int points, CancellationToken cancellationToken)
    {
        if (points <= 0) return;
        await AddPointsAsync(studentId, points, cancellationToken);
    }

    private async Task AddPointsAsync(Guid studentId, int amount, CancellationToken cancellationToken)
    {
        var record = await db.StudentPoints
            .FirstOrDefaultAsync(p => p.StudentProfileId == studentId, cancellationToken);

        if (record is null)
        {
            db.StudentPoints.Add(new StudentPoints
            {
                StudentProfileId = studentId,
                TotalPoints = amount
            });
        }
        else
        {
            record.TotalPoints += amount;
            record.LastUpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task UpdateStreakAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var streak = await db.StudentStreaks
            .FirstOrDefaultAsync(s => s.StudentProfileId == studentId, cancellationToken);

        if (streak is null)
        {
            streak = new StudentStreak
            {
                StudentProfileId = studentId,
                CurrentStreak = 1,
                BestStreak = 1,
                LastActiveDate = today
            };

            db.StudentStreaks.Add(streak);
        }
        else
        {
            if (streak.LastActiveDate == today)
                return;

            if (streak.LastActiveDate == today.AddDays(-1))
                streak.CurrentStreak++;
            else
                streak.CurrentStreak = 1;

            if (streak.CurrentStreak > streak.BestStreak)
                streak.BestStreak = streak.CurrentStreak;

            streak.LastActiveDate = today;
        }
    }

    public async Task<Result<List<LeaderboardItemDto>>> GetLeaderboardAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<LeaderboardItemDto>>.Fail("Class not found.");

        var leaderboard = await db.StudentPoints
            .Where(p => p.StudentProfile.ClassId == classId)
            .OrderByDescending(p => p.TotalPoints)
            .Select(p => new LeaderboardItemDto
            {
                StudentId = p.StudentProfileId,
                StudentName = p.StudentProfile.FirstName + " " + p.StudentProfile.LastName,
                Points = p.TotalPoints
            })
            .ToListAsync(cancellationToken);

        return Result<List<LeaderboardItemDto>>.Ok(leaderboard);
    }
}
