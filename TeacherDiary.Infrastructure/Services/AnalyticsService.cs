using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Analytics;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class AnalyticsService(AppDbContext db, ICurrentUser currentUser) : IAnalyticsService
{
    public async Task<Result<ClassAnalyticsDto>> GetClassAnalyticsAsync(
        Guid classId,
        int days,
        CancellationToken cancellationToken)
    {
        if (days <= 0) days = 30;
        if (days > 90) days = 90;

        var classExists = await db.Classes.AnyAsync(
            c => c.Id == classId && c.OrganizationId == currentUser.OrganizationId && !c.IsDeleted,
            cancellationToken);

        if (!classExists)
            return Result<ClassAnalyticsDto>.Fail("Class not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-(days - 1));
        var sevenDaysAgo = today.AddDays(-6);

        var students = await db.Students
            .AsNoTracking()
            .Where(s => s.ClassId == classId && s.IsActive && !s.IsDeleted)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(s => s.Id).ToList();

        var activityLogs = studentIds.Count == 0
            ? new List<Domain.Entities.ActivityLog>()
            : await db.ActivityLogs
                .AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentProfileId) && a.Date >= from && a.Date <= today)
                .ToListAsync(cancellationToken);

        var lastActivityByStudent = studentIds.Count == 0
            ? new Dictionary<Guid, DateTime?>()
            : await db.ActivityLogs
                .AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentProfileId))
                .GroupBy(a => a.StudentProfileId)
                .Select(g => new { StudentId = g.Key, LastAt = (DateTime?)g.Max(a => a.CreatedAt) })
                .ToDictionaryAsync(x => x.StudentId, x => x.LastAt, cancellationToken);

        var thirtyDaysAgo = today.AddDays(-29);

        var pagesLast30 = studentIds.Count == 0
            ? 0
            : await db.ActivityLogs
                .AsNoTracking()
                .Where(a => studentIds.Contains(a.StudentProfileId)
                         && a.ActivityType == ActivityType.ReadingProgress
                         && a.Date >= thirtyDaysAgo
                         && a.Date <= today)
                .SumAsync(a => a.PagesRead ?? 0, cancellationToken);

        var assignmentProgress = await db.AssignmentProgress
            .AsNoTracking()
            .Include(ap => ap.Assignment)
            .Where(ap => ap.Assignment.ClassId == classId
                      && !ap.Assignment.IsDeleted
                      && studentIds.Contains(ap.StudentProfileId))
            .ToListAsync(cancellationToken);

        var readingProgress = studentIds.Count == 0
            ? new List<Domain.Entities.ReadingProgress>()
            : await db.ReadingProgress
                .AsNoTracking()
                .Where(rp => studentIds.Contains(rp.StudentProfileId) && !rp.AssignedBook.IsDeleted)
                .ToListAsync(cancellationToken);

        var studentPoints = studentIds.Count == 0
            ? new List<Domain.Entities.StudentPoints>()
            : await db.StudentPoints
                .AsNoTracking()
                .Where(sp => studentIds.Contains(sp.StudentProfileId))
                .ToListAsync(cancellationToken);

        var studentStreaks = studentIds.Count == 0
            ? new List<Domain.Entities.StudentStreak>()
            : await db.StudentStreaks
                .AsNoTracking()
                .Where(ss => studentIds.Contains(ss.StudentProfileId))
                .ToListAsync(cancellationToken);

        // ActivityTimeline
        var logsByDate = activityLogs
            .GroupBy(a => a.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var timeline = new List<DailyActivityDto>(days);
        for (var i = 0; i < days; i++)
        {
            var date = from.AddDays(i);
            if (logsByDate.TryGetValue(date, out var dayLogs))
            {
                timeline.Add(new DailyActivityDto
                {
                    Date = date,
                    ActiveStudents = dayLogs.Select(l => l.StudentProfileId).Distinct().Count(),
                    PagesRead = dayLogs.Sum(l => l.PagesRead ?? 0),
                    PointsEarned = dayLogs.Sum(l => l.PointsEarned ?? 0),
                    CompletedAssignments = dayLogs.Count(l => l.ActivityType == ActivityType.AssignmentCompleted)
                });
            }
            else
            {
                timeline.Add(new DailyActivityDto { Date = date });
            }
        }

        // StudentEngagement
        var recentLogs = activityLogs.Where(a => a.Date >= sevenDaysAgo).ToList();
        var recentByStudent = recentLogs
            .GroupBy(a => a.StudentProfileId)
            .ToDictionary(g => g.Key, g => g.ToList());


        var pointsMap = studentPoints.ToDictionary(sp => sp.StudentProfileId, sp => sp.TotalPoints);
        var streakMap = studentStreaks.ToDictionary(ss => ss.StudentProfileId, ss => ss.CurrentStreak);

        var engagement = students
            .Select(s =>
            {
                var logs = recentByStudent.GetValueOrDefault(s.Id, new List<Domain.Entities.ActivityLog>());
                return new StudentEngagementDto
                {
                    StudentId = s.Id,
                    StudentName = s.Name,
                    DaysActiveLast7 = logs.Select(l => l.Date).Distinct().Count(),
                    TotalPoints = pointsMap.GetValueOrDefault(s.Id),
                    CurrentStreak = streakMap.GetValueOrDefault(s.Id),
                    ActiveToday = logs.Any(l => l.Date == today),
                    LastActivityAt = lastActivityByStudent.GetValueOrDefault(s.Id)
                };
            })
            .OrderByDescending(e => e.TotalPoints)
            .ToList();

        // SubjectCompletion
        var subjectCompletion = assignmentProgress
            .GroupBy(ap => ap.Assignment.Subject)
            .Select(g => new SubjectCompletionDto
            {
                Subject = g.Key,
                TotalRows = g.Count(),
                CompletedRows = g.Count(ap => ap.Status == ProgressStatus.Completed),
                CompletionRate = g.Count() > 0
                    ? g.Count(ap => ap.Status == ProgressStatus.Completed) / (double)g.Count()
                    : 0
            })
            .OrderBy(s => s.Subject)
            .ToList();

        // ReadingStats
        var readingStats = new ReadingStatsDto
        {
            NotStartedCount = readingProgress.Count(r => r.Status == ProgressStatus.NotStarted),
            InProgressCount = readingProgress.Count(r => r.Status == ProgressStatus.InProgress),
            CompletedCount = readingProgress.Count(r => r.Status == ProgressStatus.Completed),
            PagesReadLast7Days = activityLogs
                .Where(a => a.ActivityType == ActivityType.ReadingProgress && a.Date >= sevenDaysAgo)
                .Sum(a => a.PagesRead ?? 0),
            PagesReadLast30Days = pagesLast30
        };

        return Result<ClassAnalyticsDto>.Ok(new ClassAnalyticsDto
        {
            ActivityTimeline = timeline,
            StudentEngagement = engagement,
            SubjectCompletion = subjectCompletion,
            ReadingStats = readingStats
        });
    }
}
