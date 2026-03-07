using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class LearningActivityService(AppDbContext db) : ILearningActivityService
{
    public async Task<Guid> CreateForAssignedBookAsync(AssignedBook assignedBook, CancellationToken cancellationToken)
    {
        var book = await db.Books
            .Where(b => b.Id == assignedBook.BookId)
            .Select(b=> new { b.Title, b.TotalPages })
            .FirstAsync(cancellationToken);

        var activity = new LearningActivity
        {
            ClassId = assignedBook.ClassId,
            Type = LearningActivityType.Reading,
            Title = book.Title,
            TargetValue = book.TotalPages,
            Description = $"Reading: {book}",
            StartDateUtc = assignedBook.StartDateUtc,
            DueDateUtc = assignedBook.EndDateUtc,
            AssignedBookId = assignedBook.Id
        };

        db.LearningActivities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        var studentIds = await db.Students
            .Where(s => s.ClassId == assignedBook.ClassId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var rows = studentIds.Select(s => new StudentLearningActivityProgress
        {
            StudentProfileId = s,
            LearningActivityId = activity.Id,
            Status = ProgressStatus.NotStarted,
            CurrentValue = 0
        });

        db.StudentLearningActivityProgress.AddRange(rows);
        return activity.Id;
    }

    public async Task<Guid> CreateForAssignmentAsync(Assignment assignment, CancellationToken cancellationToken)
    {
        var activity = new LearningActivity
        {
            ClassId = assignment.ClassId,
            Type = LearningActivityType.Assignment,
            Title = assignment.Title,
            Description = assignment.Description,
            DueDateUtc = assignment.DueDate,
            AssignmentId = assignment.Id
        };

        db.LearningActivities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        var studentIds = await db.Students
            .Where(s => s.ClassId == assignment.ClassId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var rows = studentIds.Select(s => new StudentLearningActivityProgress
        {
            StudentProfileId = s,
            LearningActivityId = activity.Id,
            Status = ProgressStatus.NotStarted
        });

        db.StudentLearningActivityProgress.AddRange(rows);
        return activity.Id;
    }

    public async Task<Guid> CreateForChallengeAsync(Challenge challenge, CancellationToken cancellationToken)
    {
        var activity = new LearningActivity
        {
            ClassId = challenge.ClassId,
            Type = LearningActivityType.Challenge,
            Title = challenge.Title,
            Description = challenge.Description,
            StartDateUtc = challenge.StartDate,
            DueDateUtc = challenge.EndDate,
            ChallengeId = challenge.Id,
            TargetValue = challenge.TargetValue
        };

        db.LearningActivities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);

        var studentIds = await db.Students
            .Where(s => s.ClassId == challenge.ClassId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var rows = studentIds.Select(s => new StudentLearningActivityProgress
        {
            StudentProfileId = s,
            LearningActivityId = activity.Id,
            TargetValue = challenge.TargetValue
        });

        db.StudentLearningActivityProgress.AddRange(rows);
        return activity.Id;
    }

    public async Task UpdateReadingProgressAsync(
        Guid studentId,
        Guid assignedBookId,
        int currentPage,
        CancellationToken cancellationToken)
    {
        var progress = await db.StudentLearningActivityProgress
            .Include(p => p.LearningActivity)
            .FirstOrDefaultAsync(p =>
                p.StudentProfileId == studentId &&
                p.LearningActivity.AssignedBookId == assignedBookId,
                cancellationToken);

        if (progress is null) return;

        progress.StartedAt ??= DateTime.UtcNow;

        progress.CurrentValue = currentPage;
        progress.Status = ProgressStatus.InProgress;
        progress.LastUpdatedAt = DateTime.UtcNow;

        if (progress.TargetValue.HasValue && currentPage >= progress.TargetValue.Value)
        {
            progress.Status = ProgressStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
        }
    }

    public async Task UpdateAssignmentProgressAsync(
        Guid studentId,
        Guid assignmentId,
        bool completed,
        int? score,
        CancellationToken cancellationToken)
    {
        var progress = await db.StudentLearningActivityProgress
            .Include(p => p.LearningActivity)
            .FirstOrDefaultAsync(p =>
                p.StudentProfileId == studentId &&
                p.LearningActivity.AssignmentId == assignmentId,
                cancellationToken);

        if (progress is null) return;

        progress.StartedAt ??= DateTime.UtcNow;

        progress.Status = completed
            ? ProgressStatus.Completed
            : ProgressStatus.InProgress;

        progress.Score = score;
        progress.LastUpdatedAt = DateTime.UtcNow;

        if (completed && progress.CompletedAt is null)
            progress.CompletedAt = DateTime.UtcNow;
    }

    public async Task UpdateChallengeProgressAsync(
        Guid studentId,
        Guid challengeId,
        int currentValue,
        bool completed,
        CancellationToken cancellationToken)
    {
        var progress = await db.StudentLearningActivityProgress
            .Include(p => p.LearningActivity)
            .FirstOrDefaultAsync(p =>
                p.StudentProfileId == studentId &&
                p.LearningActivity.ChallengeId == challengeId,
                cancellationToken);

        if (progress is null) return;

        progress.CurrentValue = currentValue;
        progress.LastUpdatedAt = DateTime.UtcNow;

        if (completed)
        {
            progress.Status = ProgressStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            progress.Status = ProgressStatus.InProgress;
        }
    }
}
