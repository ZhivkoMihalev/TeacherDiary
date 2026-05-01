using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class StudentSelfService(
    AppDbContext db,
    ICurrentUser currentUser,
    IActivityService activityService,
    ILearningActivityService learningActivityService,
    IBadgeService badgeService,
    IEventDispatcher eventDispatcher) : IStudentSelfService
{
    private async Task<StudentProfile?> FindProfileAsync(CancellationToken cancellationToken)
        => await db.Students.FirstOrDefaultAsync(s => s.UserId == currentUser.UserId, cancellationToken);

    public async Task<Result<StudentDetailsDto>> GetMyDetailsAsync(CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<StudentDetailsDto>.Fail("Student profile not found.");

        var studentId = profile.Id;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-6);

        var reading = await db.ReadingProgress
            .AsNoTracking()
            .Where(r => r.StudentProfileId == studentId)
            .Select(r => new StudentReadingDto
            {
                AssignedBookId = r.AssignedBookId,
                BookTitle = r.AssignedBook.Book.Title,
                CurrentPage = r.CurrentPage,
                TotalPages = r.TotalPages,
                Status = r.Status,
                EndDateUtc = r.AssignedBook.EndDateUtc,
                IsExpired = r.AssignedBook.EndDateUtc.HasValue && r.AssignedBook.EndDateUtc.Value < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        var assignments = await db.AssignmentProgress
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId)
            .Select(a => new StudentAssignmentDto
            {
                AssignmentId = a.AssignmentId,
                Title = a.Assignment.Title,
                Subject = a.Assignment.Subject,
                Status = a.Status,
                DueDate = a.Assignment.DueDate,
                IsExpired = a.Assignment.DueDate.HasValue && a.Assignment.DueDate.Value < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        var activityLast7 = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId && a.Date >= from && a.Date <= today)
            .OrderBy(a => a.Date).ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var activityByDay = activityLast7
            .Select(a => new StudentActivityEntryDto
            {
                Date = a.Date,
                Description = a.ActivityType switch
                {
                    ActivityType.ReadingProgress => $"Прочел {a.PagesRead ?? 0} стр.",
                    ActivityType.AssignmentCompleted => "Завърши задача",
                    ActivityType.AssignmentStarted => "Стартира задача",
                    ActivityType.ChallengeCompleted => "Завърши предизвикателство",
                    ActivityType.ChallengeProgressUpdated => "Актуализира предизвикателство",
                    ActivityType.LearningActivityCompleted => "Завърши учебна дейност",
                    ActivityType.LearningActivityStarted => "Стартира учебна дейност",
                    _ => "Активност"
                },
                PointsEarned = a.PointsEarned ?? 0
            })
            .ToList();

        var lastActivity = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var stats = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId)
            .GroupBy(a => 1)
            .Select(g => new
            {
                PagesRead = g.Where(a => a.ActivityType == ActivityType.ReadingProgress).Sum(a => a.PagesRead ?? 0),
                AssignmentsCompleted = g.Count(a => a.ActivityType == ActivityType.AssignmentCompleted),
                TotalPoints = g.Sum(a => a.PointsEarned ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var learningActivities = await db.StudentLearningActivityProgress
            .AsNoTracking()
            .Where(p => p.StudentProfileId == studentId)
            .Select(p => new StudentLearningActivityDto
            {
                LearningActivityId = p.LearningActivityId,
                Title = p.LearningActivity.Title,
                Type = p.LearningActivity.Type,
                Status = p.Status,
                CurrentValue = p.CurrentValue,
                TargetValue = p.TargetValue,
                Score = p.Score,
                DueDateUtc = p.LearningActivity.DueDateUtc,
                IsExpired = p.LearningActivity.DueDateUtc.HasValue && p.LearningActivity.DueDateUtc.Value < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        var challenges = await db.ChallengeProgress
            .AsNoTracking()
            .Where(cp => cp.StudentProfileId == studentId)
            .Select(cp => new StudentChallengeDto
            {
                ChallengeId = cp.ChallengeId,
                Title = cp.Challenge.Title,
                Description = cp.Challenge.Description,
                TargetDescription = cp.Challenge.TargetDescription,
                TargetValue = cp.Challenge.TargetValue,
                CurrentValue = cp.CurrentValue,
                Started = cp.StartedAt != null,
                Completed = cp.Completed,
                EndDate = cp.Challenge.EndDate,
                IsExpired = cp.Challenge.EndDate < DateTime.UtcNow
            })
            .OrderBy(c => c.Completed)
            .ThenBy(c => c.EndDate)
            .ToListAsync(cancellationToken);

        var bestStreak = await db.StudentStreaks
            .AsNoTracking()
            .Where(s => s.StudentProfileId == studentId)
            .Select(s => s.BestStreak)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<StudentDetailsDto>.Ok(new StudentDetailsDto
        {
            StudentId = profile.Id,
            StudentName = $"{profile.FirstName} {profile.LastName}",
            IsActive = profile.IsActive,
            LastActivityAt = lastActivity,
            TotalPagesRead = stats?.PagesRead ?? 0,
            CompletedAssignments = stats?.AssignmentsCompleted ?? 0,
            TotalPoints = stats?.TotalPoints ?? 0,
            TopMedalCode = BadgeCodes.GetStreakMedalCode(bestStreak),
            TopPointsMedalCode = BadgeCodes.GetPointsMedalCode(stats?.TotalPoints ?? 0),
            Reading = reading,
            Assignments = assignments,
            ActivityLast7Days = activityByDay,
            LearningActivities = learningActivities,
            Challenges = challenges
        });
    }

    public async Task<Result<bool>> UpdateReadingProgressAsync(Guid assignedBookId, int currentPage, CancellationToken cancellationToken)
    {
        if (currentPage < 0)
            return Result<bool>.Fail("Current page cannot be negative.");

        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<bool>.Fail("Student profile not found.");

        var studentId = profile.Id;

        var progress = await db.ReadingProgress
            .Include(p => p.AssignedBook)
            .FirstOrDefaultAsync(p => p.StudentProfileId == studentId && p.AssignedBookId == assignedBookId, cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Progress not found.");

        if (progress.AssignedBook.EndDateUtc.HasValue && progress.AssignedBook.EndDateUtc.Value < DateTime.UtcNow)
            return Result<bool>.Fail("Deadline has passed. Reading progress is locked.");

        if (currentPage < progress.CurrentPage)
            return Result<bool>.Fail("Current page cannot be less than previous progress.");

        var previousPage = progress.CurrentPage;
        var wasAlreadyCompleted = progress.Status == ProgressStatus.Completed;

        if (progress.StartedAt == null)
            progress.StartedAt = DateTime.UtcNow;

        progress.CurrentPage = currentPage;
        progress.LastUpdatedAt = DateTime.UtcNow;

        if (progress.TotalPages.HasValue && currentPage >= progress.TotalPages.Value)
        {
            progress.Status = ProgressStatus.Completed;
            if (!wasAlreadyCompleted)
                progress.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            progress.Status = ProgressStatus.InProgress;
        }

        var pagesDelta = currentPage - previousPage;
        var bookCompleted = !wasAlreadyCompleted && progress.Status == ProgressStatus.Completed;

        await activityService.LogReadingAsync(studentId, assignedBookId, pagesDelta, bookCompleted, progress.AssignedBook.Points, cancellationToken);
        await learningActivityService.UpdateReadingProgressAsync(studentId, assignedBookId, currentPage, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        if (bookCompleted)
            await eventDispatcher.PublishAsync(
                new BookCompletedEvent(studentId, assignedBookId, progress.AssignedBook.ClassId),
                cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> StartAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<bool>.Fail("Student profile not found.");

        var progress = await db.AssignmentProgress
            .FirstOrDefaultAsync(p => p.StudentProfileId == profile.Id && p.AssignmentId == assignmentId, cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Assignment not found.");

        if (progress.Status != ProgressStatus.NotStarted)
            return Result<bool>.Ok(true);

        progress.Status = ProgressStatus.InProgress;
        progress.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CompleteAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<bool>.Fail("Student profile not found.");

        var studentId = profile.Id;

        var progress = await db.AssignmentProgress
            .Include(p => p.Assignment)
            .FirstOrDefaultAsync(p => p.StudentProfileId == studentId && p.AssignmentId == assignmentId, cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Assignment not found.");

        if (progress.Status == ProgressStatus.Completed)
            return Result<bool>.Ok(true);

        progress.Status = ProgressStatus.Completed;
        progress.CompletedAt = DateTime.UtcNow;

        await activityService.LogAssignmentCompletedAsync(studentId, assignmentId, progress.Assignment.Points, cancellationToken);
        await learningActivityService.UpdateAssignmentProgressAsync(studentId, assignmentId, true, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await eventDispatcher.PublishAsync(
            new AssignmentCompletedEvent(studentId, assignmentId, progress.Assignment.ClassId),
            cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> StartChallengeAsync(Guid challengeId, CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<bool>.Fail("Student profile not found.");

        var progress = await db.ChallengeProgress
            .FirstOrDefaultAsync(cp => cp.StudentProfileId == profile.Id && cp.ChallengeId == challengeId, cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Challenge not found.");

        if (progress.StartedAt is not null)
            return Result<bool>.Ok(true);

        progress.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> CompleteChallengeAsync(Guid challengeId, CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<bool>.Fail("Student profile not found.");

        var studentId = profile.Id;

        var progress = await db.ChallengeProgress
            .Include(cp => cp.Challenge)
            .FirstOrDefaultAsync(cp => cp.StudentProfileId == studentId && cp.ChallengeId == challengeId, cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Challenge not found.");

        if (progress.Completed)
            return Result<bool>.Ok(true);

        progress.Completed = true;
        progress.CompletedAt = DateTime.UtcNow;
        if (progress.Challenge.TargetValue > 0)
            progress.CurrentValue = Math.Max(progress.CurrentValue, progress.Challenge.TargetValue);

        await activityService.LogChallengeCompletedAsync(studentId, challengeId, progress.Challenge.Points, cancellationToken);
        await learningActivityService.UpdateChallengeProgressAsync(studentId, challengeId, progress.CurrentValue, true, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await eventDispatcher.PublishAsync(
            new ChallengeCompletedEvent(studentId, challengeId, progress.Challenge.ClassId),
            cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<StudentBadgeDto>>> GetMyBadgesAsync(CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile is null)
            return Result<List<StudentBadgeDto>>.Fail("Student profile not found.");

        var badges = await db.StudentBadges
            .AsNoTracking()
            .Where(sb => sb.StudentProfileId == profile.Id)
            .OrderByDescending(sb => sb.AwardedAt)
            .Select(sb => new StudentBadgeDto
            {
                Code = sb.Badge.Code,
                Name = sb.Badge.Name,
                Description = sb.Badge.Description,
                Icon = sb.Badge.Icon,
                AwardedAt = sb.AwardedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<StudentBadgeDto>>.Ok(badges);
    }
}