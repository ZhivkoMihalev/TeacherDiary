using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ActivityService(
    AppDbContext db,
    IGamificationService gamificationService,
    IBadgeService badgeService,
    ILearningActivityService learningActivityService,
    IEventDispatcher eventDispatcher) : IActivityService
{
    public async Task LogReadingAsync(
        Guid studentId,
        Guid assignedBookId,
        int pagesRead,
        bool bookCompleted,
        int bookPoints,
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
            PagesRead = pagesRead,
            PointsEarned = bookCompleted ? bookPoints : 0
        });

        await UpdateChallengeProgressAsync(studentId, TargetType.Pages, pagesRead, cancellationToken);

        if (bookCompleted)
        {
            await UpdateChallengeProgressAsync(studentId, TargetType.Books, 1, cancellationToken);
            await gamificationService.AddReadingPointsAsync(studentId, bookPoints, cancellationToken);
        }

        await gamificationService.UpdateStreakAsync(studentId, cancellationToken);
        await badgeService.EvaluateAsync(studentId, cancellationToken);
    }

    public async Task LogAssignmentCompletedAsync(
        Guid studentId,
        Guid assignmentId,
        int points,
        CancellationToken cancellationToken)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.AssignmentCompleted,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = assignmentId,
            PointsEarned = points
        });

        await UpdateChallengeProgressAsync(studentId, TargetType.Assignments, 1, cancellationToken);

        await gamificationService.AddAssignmentPointsAsync(studentId, points, cancellationToken);
        await gamificationService.UpdateStreakAsync(studentId, cancellationToken);
        await badgeService.EvaluateAsync(studentId, cancellationToken);
    }

    public async Task LogChallengeCompletedAsync(
        Guid studentId,
        Guid challengeId,
        int points,
        CancellationToken cancellationToken)
    {
        if (points > 0)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                StudentProfileId = studentId,
                ActivityType = ActivityType.ChallengeCompleted,
                ReferenceType = ActivityReferenceType.Challenge,
                ReferenceId = challengeId,
                PointsEarned = points
            });
            await gamificationService.AddChallengePointsAsync(studentId, points, cancellationToken);
        }

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
                await gamificationService.AddChallengePointsAsync(studentId, row.Challenge.Points, cancellationToken);

                if (row.Challenge.Points > 0)
                {
                    db.ActivityLogs.Add(new ActivityLog
                    {
                        StudentProfileId = studentId,
                        ActivityType = ActivityType.ChallengeCompleted,
                        ReferenceType = ActivityReferenceType.Challenge,
                        ReferenceId = row.ChallengeId,
                        PointsEarned = row.Challenge.Points
                    });
                }

                await eventDispatcher.PublishAsync(
                    new ChallengeCompletedEvent(studentId, row.ChallengeId, row.Challenge.ClassId),
                    cancellationToken);
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