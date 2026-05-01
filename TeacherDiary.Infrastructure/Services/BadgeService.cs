using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class BadgeService(AppDbContext db, IEventDispatcher eventDispatcher) : IBadgeService
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

        {
            var streak = await db.StudentStreaks
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StudentProfileId == studentId, cancellationToken);

            if (streak is not null)
            {
                foreach (var (days, code) in BadgeCodes.StreakTiers)
                {
                    if (!awardedSet.Contains(code) && streak.BestStreak >= days)
                        badgesToAward.Add(code);
                }
            }
        }

        {
            var totalPoints = await db.ActivityLogs
                .Where(a => a.StudentProfileId == studentId)
                .SumAsync(a => a.PointsEarned ?? 0, cancellationToken);

            foreach (var (threshold, code) in BadgeCodes.PointsTiers)
            {
                if (!awardedSet.Contains(code) && totalPoints >= threshold)
                    badgesToAward.Add(code);
            }
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

        foreach (var badge in badgeEntities)
            await eventDispatcher.PublishAsync(new BadgeEarnedEvent(studentId, badge.Name), cancellationToken);
    }
}