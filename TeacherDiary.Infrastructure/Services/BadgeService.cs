using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class BadgeService(AppDbContext db) : IBadgeService
{
    public async Task EvaluateAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var studentExists = await db.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == studentId, cancellationToken);

        if (!studentExists)
            return;

        var awardedBadgeCodes = await db.StudentBadges
            .Where(sb => sb.StudentProfileId == studentId)
            .Select(sb => sb.Badge.Code)
            .ToListAsync(cancellationToken);

        var awardedSet = awardedBadgeCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var badgesToAward = new List<string>();

        // 1. First book completed
        if (!awardedSet.Contains(BadgeCodes.FirstBookCompleted))
        {
            var hasCompletedBook = await db.ReadingProgress
                .AnyAsync(r =>
                    r.StudentProfileId == studentId &&
                    r.Status == ProgressStatus.Completed,
                    cancellationToken);

            if (hasCompletedBook)
                badgesToAward.Add(BadgeCodes.FirstBookCompleted);
        }

        // 2. Read 100 pages
        if (!awardedSet.Contains(BadgeCodes.Read100Pages))
        {
            var totalPagesRead = await db.ActivityLogs
                .Where(a =>
                    a.StudentProfileId == studentId &&
                    a.ActivityType == ActivityType.ReadingProgress)
                .SumAsync(a => a.PagesRead ?? 0, cancellationToken);

            if (totalPagesRead >= 100)
                badgesToAward.Add(BadgeCodes.Read100Pages);
        }

        // 3. Complete 5 assignments
        if (!awardedSet.Contains(BadgeCodes.Complete5Assignments))
        {
            var completedAssignments = await db.AssignmentProgress
                .CountAsync(a =>
                    a.StudentProfileId == studentId &&
                    a.Status == ProgressStatus.Completed,
                    cancellationToken);

            if (completedAssignments >= 5)
                badgesToAward.Add(BadgeCodes.Complete5Assignments);
        }

        // 4. 7 day streak
        if (!awardedSet.Contains(BadgeCodes.SevenDayStreak))
        {
            var streak = await db.StudentStreaks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentProfileId == studentId, cancellationToken);

            if (streak is not null && streak.BestStreak >= 7)
                badgesToAward.Add(BadgeCodes.SevenDayStreak);
        }

        // 5. Reach 100 points
        if (!awardedSet.Contains(BadgeCodes.Reach100Points))
        {
            var points = await db.StudentPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.StudentProfileId == studentId, cancellationToken);

            if (points is not null && points.TotalPoints >= 100)
                badgesToAward.Add(BadgeCodes.Reach100Points);
        }

        if (badgesToAward.Count == 0)
            return;

        var badgeEntities = await db.Badges
            .Where(b => badgesToAward.Contains(b.Code))
            .ToListAsync(cancellationToken);

        var studentBadges = badgeEntities.Select(b => new StudentBadge
        {
            StudentProfileId = studentId,
            BadgeId = b.Id,
            AwardedAt = DateTime.UtcNow
        });

        db.StudentBadges.AddRange(studentBadges);
    }
}
