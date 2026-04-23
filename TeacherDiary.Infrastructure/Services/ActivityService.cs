using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ActivityService(
    AppDbContext db,
    IGamificationService gamificationService,
    IBadgeService badgeService,
    ILearningActivityService learningActivityService) : IActivityService
{
    public async Task LogReadingAsync(
        Guid studentId,
        Guid assignedBookId,
        int pagesRead,
        bool bookCompleted,
        CancellationToken cancellationToken)
    {
        if (pagesRead <= 0)
            return;

        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.ReadingProgress,
            ReferenceType = ActivityReferenceType.AssignedBook,
            ReferenceId = assignedBookId,
            PagesRead = pagesRead
        });

        await UpdateChallengeProgressAsync(studentId, TargetType.Pages, pagesRead, cancellationToken);

        if (bookCompleted)
            await UpdateChallengeProgressAsync(studentId, TargetType.Books, 1, cancellationToken);

        await gamificationService.AddReadingPointsAsync(studentId, pagesRead, cancellationToken);
        await gamificationService.UpdateStreakAsync(studentId, cancellationToken);
        await badgeService.EvaluateAsync(studentId, cancellationToken);
    }

    public async Task LogAssignmentCompletedAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.AssignmentCompleted,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = assignmentId
        });

        await UpdateChallengeProgressAsync(studentId, TargetType.Assignments, 1, cancellationToken);

        await gamificationService.AddAssignmentPointsAsync(studentId, cancellationToken);
        await gamificationService.UpdateStreakAsync(studentId, cancellationToken);
        await badgeService.EvaluateAsync(studentId, cancellationToken);
    }

    private async Task UpdateChallengeProgressAsync(
        Guid studentId,
        TargetType targetType,
        int increment,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var rows = await db.ChallengeProgress
            .Include(cp => cp.Challenge)
            .Where(cp =>
                cp.StudentProfileId == studentId &&
                !cp.Completed &&
                cp.Challenge.TargetType == targetType &&
                cp.Challenge.StartDate <= now &&
                cp.Challenge.EndDate >= now)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.CurrentValue += increment;

            if (row.CurrentValue >= row.Challenge.TargetValue)
            {
                row.Completed = true;
                row.CompletedAt = now;
            }

            await learningActivityService.UpdateChallengeProgressAsync(
                studentId,
                row.ChallengeId,
                row.CurrentValue,
                row.Completed,
                cancellationToken);
        }
    }
}
