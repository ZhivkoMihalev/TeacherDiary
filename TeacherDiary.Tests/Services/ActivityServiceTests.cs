using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class ActivityServiceTests
{
    private readonly Mock<IGamificationService> _gamificationMock = new();
    private readonly Mock<IBadgeService> _badgeMock = new();
    private readonly Mock<ILearningActivityService> _learningActivityMock = new();
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private ActivityService CreateService(AppDbContext db)
        => new(db, _gamificationMock.Object, _badgeMock.Object, _learningActivityMock.Object, _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // LogReadingAsync — early return when pagesRead <= 0
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogReadingAsync_WhenPagesReadIsZero_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogReadingAsync(Guid.NewGuid(), Guid.NewGuid(), 0, false, 0, CancellationToken.None);

        Assert.Empty(db.ActivityLogs.Local);
        _gamificationMock.VerifyNoOtherCalls();
        _badgeMock.VerifyNoOtherCalls();
        _learningActivityMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesReadIsNegative_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogReadingAsync(Guid.NewGuid(), Guid.NewGuid(), -5, false, 0, CancellationToken.None);

        Assert.Empty(db.ActivityLogs.Local);
        _gamificationMock.VerifyNoOtherCalls();
        _badgeMock.VerifyNoOtherCalls();
        _learningActivityMock.VerifyNoOtherCalls();
    }

    // -----------------------------------------------------------------------
    // LogReadingAsync — book not yet completed
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogReadingAsync_WhenBookNotCompleted_AddsActivityLogWithZeroPoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();
        var assignedBookId = Guid.NewGuid();

        await service.LogReadingAsync(studentId, assignedBookId, 10, false, 50, CancellationToken.None);

        var log = Assert.Single(db.ActivityLogs.Local);
        Assert.Equal(studentId, log.StudentProfileId);
        Assert.Equal(ActivityType.ReadingProgress, log.ActivityType);
        Assert.Equal(ActivityReferenceType.AssignedBook, log.ReferenceType);
        Assert.Equal(assignedBookId, log.ReferenceId);
        Assert.Equal(10, log.PagesRead);
        Assert.Equal(0, log.PointsEarned);
    }

    [Fact]
    public async Task LogReadingAsync_WhenBookNotCompleted_DoesNotCallAddReadingPoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogReadingAsync(Guid.NewGuid(), Guid.NewGuid(), 10, false, 50, CancellationToken.None);

        _gamificationMock.Verify(
            g => g.AddReadingPointsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogReadingAsync_WhenBookNotCompleted_UpdatesStreakAndEvaluatesBadges()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.LogReadingAsync(studentId, Guid.NewGuid(), 10, false, 0, CancellationToken.None);

        _gamificationMock.Verify(g => g.UpdateStreakAsync(studentId, CancellationToken.None), Times.Once);
        _badgeMock.Verify(b => b.EvaluateAsync(studentId, CancellationToken.None), Times.Once);
    }

    // -----------------------------------------------------------------------
    // LogReadingAsync — book completed
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogReadingAsync_WhenBookCompleted_AddsActivityLogWithBookPoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogReadingAsync(Guid.NewGuid(), Guid.NewGuid(), 10, true, 100, CancellationToken.None);

        var log = Assert.Single(db.ActivityLogs.Local);
        Assert.Equal(100, log.PointsEarned);
    }

    [Fact]
    public async Task LogReadingAsync_WhenBookCompleted_CallsAddReadingPoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.LogReadingAsync(studentId, Guid.NewGuid(), 10, true, 75, CancellationToken.None);

        _gamificationMock.Verify(g => g.AddReadingPointsAsync(studentId, 75, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LogReadingAsync_WhenBookCompleted_UpdatesStreakAndEvaluatesBadges()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.LogReadingAsync(studentId, Guid.NewGuid(), 10, true, 50, CancellationToken.None);

        _gamificationMock.Verify(g => g.UpdateStreakAsync(studentId, CancellationToken.None), Times.Once);
        _badgeMock.Verify(b => b.EvaluateAsync(studentId, CancellationToken.None), Times.Once);
    }

    // -----------------------------------------------------------------------
    // UpdateChallengeProgressAsync — Pages target (via LogReadingAsync)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogReadingAsync_WhenNoPagesChallenge_DoesNotCallLearningActivityUpdate()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogReadingAsync(Guid.NewGuid(), Guid.NewGuid(), 5, false, 0, CancellationToken.None);

        _learningActivityMock.Verify(
            l => l.UpdateChallengeProgressAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeExistsAndBelowTarget_IncrementsCurrentValue()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 100, points: 50, active: true);
        var progress = SeedChallengeProgress(db, studentId, challenge, currentValue: 10, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 20, false, 0, CancellationToken.None);

        Assert.Equal(30, progress.CurrentValue);
        Assert.False(progress.Completed);
        _learningActivityMock.Verify(
            l => l.UpdateChallengeProgressAsync(studentId, challenge.Id, 30, false, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeReachesTarget_WithPositivePoints_CompletesChallenge()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 30, points: 50, active: true);
        var progress = SeedChallengeProgress(db, studentId, challenge, currentValue: 20, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 15, false, 0, CancellationToken.None);

        Assert.True(progress.Completed);
        Assert.NotNull(progress.CompletedAt);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeReachesTarget_WithPositivePoints_AddsChallengeActivityLog()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 30, points: 50, active: true);
        SeedChallengeProgress(db, studentId, challenge, currentValue: 20, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 15, false, 0, CancellationToken.None);

        var challengeLog = db.ActivityLogs.Local
            .Single(l => l.ActivityType == ActivityType.ChallengeCompleted);
        Assert.Equal(challenge.Id, challengeLog.ReferenceId);
        Assert.Equal(50, challengeLog.PointsEarned);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeReachesTarget_WithPositivePoints_CallsAddChallengePoints()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 30, points: 50, active: true);
        SeedChallengeProgress(db, studentId, challenge, currentValue: 20, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 15, false, 0, CancellationToken.None);

        _gamificationMock.Verify(g => g.AddChallengePointsAsync(studentId, 50, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeReachesTarget_WithPositivePoints_PublishesChallengeCompletedEvent()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 30, points: 50, active: true);
        SeedChallengeProgress(db, studentId, challenge, currentValue: 20, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 15, false, 0, CancellationToken.None);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<ChallengeCompletedEvent>(ev => ev.StudentId == studentId && ev.ChallengeId == challenge.Id),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeReachesTarget_WithZeroPoints_DoesNotAddChallengeActivityLog()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 30, points: 0, active: true);
        SeedChallengeProgress(db, studentId, challenge, currentValue: 20, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 15, false, 0, CancellationToken.None);

        Assert.DoesNotContain(db.ActivityLogs.Local, l => l.ActivityType == ActivityType.ChallengeCompleted);
    }

    [Fact]
    public async Task LogReadingAsync_WhenPagesChallengeAlreadyCompleted_DoesNotIncrementCurrentValue()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 50, points: 30, active: true);
        var progress = SeedChallengeProgress(db, studentId, challenge, currentValue: 50, completed: true);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 10, false, 0, CancellationToken.None);

        Assert.Equal(50, progress.CurrentValue);
        _learningActivityMock.Verify(
            l => l.UpdateChallengeProgressAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogReadingAsync_WhenChallengeExpired_DoesNotIncrementCurrentValue()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Pages, targetValue: 50, points: 30, active: false);
        var progress = SeedChallengeProgress(db, studentId, challenge, currentValue: 0, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 10, false, 0, CancellationToken.None);

        Assert.Equal(0, progress.CurrentValue);
        _learningActivityMock.Verify(
            l => l.UpdateChallengeProgressAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // UpdateChallengeProgressAsync — Books target (via bookCompleted = true)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogReadingAsync_WhenBookCompleted_UpdatesBooksChallenge()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Books, targetValue: 5, points: 30, active: true);
        var progress = SeedChallengeProgress(db, studentId, challenge, currentValue: 2, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogReadingAsync(studentId, Guid.NewGuid(), 10, true, 50, CancellationToken.None);

        Assert.Equal(3, progress.CurrentValue);
    }

    // -----------------------------------------------------------------------
    // LogAssignmentCompletedAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogAssignmentCompletedAsync_AddsActivityLogWithCorrectProperties()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        await service.LogAssignmentCompletedAsync(studentId, assignmentId, 20, CancellationToken.None);

        var log = Assert.Single(db.ActivityLogs.Local);
        Assert.Equal(studentId, log.StudentProfileId);
        Assert.Equal(ActivityType.AssignmentCompleted, log.ActivityType);
        Assert.Equal(ActivityReferenceType.Assignment, log.ReferenceType);
        Assert.Equal(assignmentId, log.ReferenceId);
        Assert.Equal(20, log.PointsEarned);
    }

    [Fact]
    public async Task LogAssignmentCompletedAsync_CallsAddAssignmentPoints_UpdateStreak_EvaluateBadges()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.LogAssignmentCompletedAsync(studentId, Guid.NewGuid(), 20, CancellationToken.None);

        _gamificationMock.Verify(g => g.AddAssignmentPointsAsync(studentId, 20, CancellationToken.None), Times.Once);
        _gamificationMock.Verify(g => g.UpdateStreakAsync(studentId, CancellationToken.None), Times.Once);
        _badgeMock.Verify(b => b.EvaluateAsync(studentId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LogAssignmentCompletedAsync_UpdatesAssignmentsChallengeProgress()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var challenge = SeedChallenge(db, TargetType.Assignments, targetValue: 5, points: 30, active: true);
        var progress = SeedChallengeProgress(db, studentId, challenge, currentValue: 1, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.LogAssignmentCompletedAsync(studentId, Guid.NewGuid(), 20, CancellationToken.None);

        Assert.Equal(2, progress.CurrentValue);
    }

    // -----------------------------------------------------------------------
    // LogChallengeCompletedAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LogChallengeCompletedAsync_WhenPointsIsZero_DoesNotAddActivityLog()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogChallengeCompletedAsync(Guid.NewGuid(), Guid.NewGuid(), 0, CancellationToken.None);

        Assert.Empty(db.ActivityLogs.Local);
    }

    [Fact]
    public async Task LogChallengeCompletedAsync_WhenPointsIsZero_DoesNotCallAddChallengePoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.LogChallengeCompletedAsync(Guid.NewGuid(), Guid.NewGuid(), 0, CancellationToken.None);

        _gamificationMock.Verify(
            g => g.AddChallengePointsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LogChallengeCompletedAsync_WhenPointsIsZero_StillCallsUpdateStreakAndEvaluateBadges()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.LogChallengeCompletedAsync(studentId, Guid.NewGuid(), 0, CancellationToken.None);

        _gamificationMock.Verify(g => g.UpdateStreakAsync(studentId, CancellationToken.None), Times.Once);
        _badgeMock.Verify(b => b.EvaluateAsync(studentId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LogChallengeCompletedAsync_WhenPointsIsPositive_AddsActivityLogAndCallsAddChallengePoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();
        var challengeId = Guid.NewGuid();

        await service.LogChallengeCompletedAsync(studentId, challengeId, 40, CancellationToken.None);

        var log = Assert.Single(db.ActivityLogs.Local);
        Assert.Equal(studentId, log.StudentProfileId);
        Assert.Equal(ActivityType.ChallengeCompleted, log.ActivityType);
        Assert.Equal(ActivityReferenceType.Challenge, log.ReferenceType);
        Assert.Equal(challengeId, log.ReferenceId);
        Assert.Equal(40, log.PointsEarned);
        _gamificationMock.Verify(g => g.AddChallengePointsAsync(studentId, 40, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task LogChallengeCompletedAsync_WhenPointsIsPositive_CallsUpdateStreakAndEvaluateBadges()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.LogChallengeCompletedAsync(studentId, Guid.NewGuid(), 40, CancellationToken.None);

        _gamificationMock.Verify(g => g.UpdateStreakAsync(studentId, CancellationToken.None), Times.Once);
        _badgeMock.Verify(b => b.EvaluateAsync(studentId, CancellationToken.None), Times.Once);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Challenge SeedChallenge(
        AppDbContext db,
        TargetType targetType,
        int targetValue,
        int points,
        bool active)
    {
        var challenge = new Challenge
        {
            ClassId = Guid.NewGuid(),
            Title = "Test Challenge",
            Description = "Test",
            TargetType = targetType,
            TargetValue = targetValue,
            Points = points,
            StartDate = active ? DateTime.UtcNow.AddDays(-1) : DateTime.UtcNow.AddDays(-10),
            EndDate = active ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(-1)
        };
        db.Challenges.Add(challenge);
        return challenge;
    }

    private static ChallengeProgress SeedChallengeProgress(
        AppDbContext db,
        Guid studentId,
        Challenge challenge,
        int currentValue,
        bool completed)
    {
        var progress = new ChallengeProgress
        {
            StudentProfileId = studentId,
            ChallengeId = challenge.Id,
            Challenge = challenge,
            CurrentValue = currentValue,
            Completed = completed
        };
        db.ChallengeProgress.Add(progress);
        return progress;
    }
}
