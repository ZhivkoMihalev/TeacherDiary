using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class BadgeServiceTests
{
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private BadgeService CreateService(AppDbContext db)
        => new(db, _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static StudentProfile SeedStudent(AppDbContext db)
    {
        var student = new StudentProfile { FirstName = "Test", LastName = "User" };
        db.Students.Add(student);
        return student;
    }

    private static Badge SeedBadge(AppDbContext db, string code, string name = "")
    {
        var badge = new Badge
        {
            Code = code,
            Name = string.IsNullOrEmpty(name) ? code : name,
            Description = "Test badge",
            Icon = "icon"
        };
        db.Badges.Add(badge);
        return badge;
    }

    private static void SeedStudentBadge(AppDbContext db, Guid studentId, Badge badge)
    {
        db.StudentBadges.Add(new StudentBadge
        {
            StudentProfileId = studentId,
            BadgeId = badge.Id,
            Badge = badge
        });
    }

    private static void SeedReadingProgress(AppDbContext db, Guid studentId, ProgressStatus status)
    {
        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = studentId,
            AssignedBookId = Guid.NewGuid(),
            Status = status
        });
    }

    private static void SeedReadingActivityLog(AppDbContext db, Guid studentId, int pagesRead, int pointsEarned = 0)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.ReadingProgress,
            ReferenceType = ActivityReferenceType.AssignedBook,
            ReferenceId = Guid.NewGuid(),
            PagesRead = pagesRead,
            PointsEarned = pointsEarned
        });
    }

    private static void SeedActivityLog(AppDbContext db, Guid studentId, int pointsEarned)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = ActivityType.AssignmentCompleted,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = Guid.NewGuid(),
            PointsEarned = pointsEarned
        });
    }

    private static void SeedCompletedAssignmentProgress(AppDbContext db, Guid studentId, int count)
    {
        for (var i = 0; i < count; i++)
        {
            db.AssignmentProgress.Add(new AssignmentProgress
            {
                StudentProfileId = studentId,
                AssignmentId = Guid.NewGuid(),
                Status = ProgressStatus.Completed
            });
        }
    }

    private static StudentStreak SeedStreak(AppDbContext db, Guid studentId, int bestStreak)
    {
        var streak = new StudentStreak
        {
            StudentProfileId = studentId,
            BestStreak = bestStreak,
            CurrentStreak = bestStreak,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        db.StudentStreaks.Add(streak);
        return streak;
    }

    // -----------------------------------------------------------------------
    // Student existence guard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenStudentDoesNotExist_ReturnsWithoutDoingAnything()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.EvaluateAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // Early return when no badges earned
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenNoBadgeConditionsMet_AwardsNoBadgesAndDispatchesNoEvents()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        // No ReadingProgress, no ActivityLogs, no AssignmentProgress, no StudentStreak
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // FirstBookCompleted badge
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenStudentCompletedFirstBook_AwardsFirstBookBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedReadingProgress(db, student.Id, ProgressStatus.Completed);
        var badge = SeedBadge(db, BadgeCodes.FirstBookCompleted, "First Book");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        var awarded = db.StudentBadges.Local.Single();
        Assert.Equal(badge.Id, awarded.BadgeId);
        Assert.Equal(student.Id, awarded.StudentProfileId);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<BadgeEarnedEvent>(ev => ev.StudentId == student.Id && ev.BadgeName == "First Book"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_WhenFirstBookBadgeAlreadyAwarded_SkipsBookCheck()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedReadingProgress(db, student.Id, ProgressStatus.Completed);
        var badge = SeedBadge(db, BadgeCodes.FirstBookCompleted);
        SeedStudentBadge(db, student.Id, badge);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        // Only the pre-existing badge, no new one added
        Assert.Equal(1, db.StudentBadges.Count());
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EvaluateAsync_WhenReadingProgressExistsButNotCompleted_DoesNotAwardFirstBookBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedReadingProgress(db, student.Id, ProgressStatus.InProgress);
        SeedBadge(db, BadgeCodes.FirstBookCompleted);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // Read100Pages badge
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenTotalPagesReadAtLeast100_AwardsRead100PagesBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedReadingActivityLog(db, student.Id, pagesRead: 100, pointsEarned: 0);
        var badge = SeedBadge(db, BadgeCodes.Read100Pages, "100 Pages");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Single(db.StudentBadges.Local);
        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<BadgeEarnedEvent>(ev => ev.BadgeName == "100 Pages"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_WhenRead100PagesBadgeAlreadyAwarded_SkipsPagesCheck()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedReadingActivityLog(db, student.Id, pagesRead: 200, pointsEarned: 0);
        var badge = SeedBadge(db, BadgeCodes.Read100Pages);
        SeedStudentBadge(db, student.Id, badge);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Equal(1, db.StudentBadges.Count());
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // Complete5Assignments badge
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenFiveAssignmentsCompleted_AwardsAssignmentBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedCompletedAssignmentProgress(db, student.Id, count: 5);
        var badge = SeedBadge(db, BadgeCodes.Complete5Assignments, "5 Assignments");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Single(db.StudentBadges.Local);
        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<BadgeEarnedEvent>(ev => ev.BadgeName == "5 Assignments"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAssignmentBadgeAlreadyAwarded_SkipsAssignmentCheck()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedCompletedAssignmentProgress(db, student.Id, count: 5);
        var badge = SeedBadge(db, BadgeCodes.Complete5Assignments);
        SeedStudentBadge(db, student.Id, badge);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Equal(1, db.StudentBadges.Count());
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EvaluateAsync_WhenFewerThanFiveAssignmentsCompleted_DoesNotAwardAssignmentBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedCompletedAssignmentProgress(db, student.Id, count: 4);
        SeedBadge(db, BadgeCodes.Complete5Assignments);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // Streak badges
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenStudentHasNoStreak_SkipsAllStreakBadges()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        // No StudentStreak seeded
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EvaluateAsync_WhenBestStreakMeetsLowestTier_AwardsStreakBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedStreak(db, student.Id, bestStreak: 3);
        var badge = SeedBadge(db, BadgeCodes.Streak3, "Streak 3");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Single(db.StudentBadges.Local);
        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<BadgeEarnedEvent>(ev => ev.BadgeName == "Streak 3"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_WhenStreakBadgeAlreadyAwarded_DoesNotAwardAgain()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedStreak(db, student.Id, bestStreak: 3);
        var badge = SeedBadge(db, BadgeCodes.Streak3);
        SeedStudentBadge(db, student.Id, badge);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Equal(1, db.StudentBadges.Count());
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EvaluateAsync_WhenBestStreakBelowAllTiers_DoesNotAwardStreakBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedStreak(db, student.Id, bestStreak: 2); // below minimum tier of 3
        foreach (var (_, code) in BadgeCodes.StreakTiers)
            SeedBadge(db, code);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // Points badges
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenTotalPointsMeetLowestTier_AwardsPointsBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedActivityLog(db, student.Id, pointsEarned: 100);
        var badge = SeedBadge(db, BadgeCodes.Points100, "100 Points");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Single(db.StudentBadges.Local);
        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<BadgeEarnedEvent>(ev => ev.BadgeName == "100 Points"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPointsBadgeAlreadyAwarded_DoesNotAwardAgain()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedActivityLog(db, student.Id, pointsEarned: 100);
        var badge = SeedBadge(db, BadgeCodes.Points100);
        SeedStudentBadge(db, student.Id, badge);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Equal(1, db.StudentBadges.Count());
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EvaluateAsync_WhenTotalPointsBelowAllTiers_DoesNotAwardPointsBadge()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);
        SeedActivityLog(db, student.Id, pointsEarned: 50); // below 100 threshold
        foreach (var (_, code) in BadgeCodes.PointsTiers)
            SeedBadge(db, code);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Empty(db.StudentBadges.Local);
        _eventDispatcherMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // Multiple badges at once
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EvaluateAsync_WhenMultipleBadgesEarned_AwardsAllAndDispatchesSeparateEvents()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db);

        // Triggers FirstBookCompleted
        SeedReadingProgress(db, student.Id, ProgressStatus.Completed);

        // Triggers Read100Pages (but not Points100 since PointsEarned = 0)
        SeedReadingActivityLog(db, student.Id, pagesRead: 100, pointsEarned: 0);

        var bookBadge = SeedBadge(db, BadgeCodes.FirstBookCompleted, "First Book");
        var pagesBadge = SeedBadge(db, BadgeCodes.Read100Pages, "100 Pages");
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.EvaluateAsync(student.Id, CancellationToken.None);

        Assert.Equal(2, db.StudentBadges.Local.Count);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(It.IsAny<BadgeEarnedEvent>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
