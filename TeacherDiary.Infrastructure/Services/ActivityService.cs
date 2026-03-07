using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ActivityService(
    AppDbContext db,
    IGamificationService gamificationService,
    IBadgeService badgeService) : IActivityService
{
    public async Task LogReadingAsync(
        Guid studentId,
        Guid assignedBookId,
        int pagesRead,
        CancellationToken cancellationToken)
    {
        if (pagesRead <= 0)
            return;

        var activity = new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.ReadingProgress,
            ReferenceType = ActivityReferenceType.AssignedBook,
            ReferenceId = assignedBookId,
            PagesRead = pagesRead
        };

        db.ActivityLogs.Add(activity);

        await gamificationService.AddReadingPointsAsync(studentId, pagesRead, cancellationToken);
        await gamificationService.UpdateStreakAsync(studentId, cancellationToken);

        await badgeService.EvaluateAsync(studentId, cancellationToken);
    }

    public async Task LogAssignmentCompletedAsync(
        Guid studentId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var activity = new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.AssignmentCompleted,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = assignmentId
        };

        db.ActivityLogs.Add(activity);

        await gamificationService.AddAssignmentPointsAsync(studentId, cancellationToken);
        await gamificationService.UpdateStreakAsync(studentId, cancellationToken);

        await badgeService.EvaluateAsync(studentId, cancellationToken);
    }
}
