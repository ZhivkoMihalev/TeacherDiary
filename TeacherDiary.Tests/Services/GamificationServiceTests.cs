using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class GamificationServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();

    public GamificationServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private GamificationService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object, _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // AddReadingPointsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddReadingPointsAsync_WhenPointsAreZero_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.AddReadingPointsAsync(Guid.NewGuid(), 0, CancellationToken.None);

        Assert.Empty(db.StudentPoints.Local);
    }

    [Fact]
    public async Task AddReadingPointsAsync_WhenNoExistingRecord_CreatesStudentPoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.AddReadingPointsAsync(studentId, 50, CancellationToken.None);

        var record = db.StudentPoints.Local.Single();
        Assert.Equal(studentId, record.StudentProfileId);
        Assert.Equal(50, record.TotalPoints);
    }

    [Fact]
    public async Task AddReadingPointsAsync_WhenRecordExists_IncreasesTotalPoints()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = studentId, TotalPoints = 100 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.AddReadingPointsAsync(studentId, 25, CancellationToken.None);

        Assert.Equal(125, db.StudentPoints.Local.Single().TotalPoints);
    }

    // -----------------------------------------------------------------------
    // AddAssignmentPointsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddAssignmentPointsAsync_WhenPointsAreZero_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.AddAssignmentPointsAsync(Guid.NewGuid(), 0, CancellationToken.None);

        Assert.Empty(db.StudentPoints.Local);
    }

    [Fact]
    public async Task AddAssignmentPointsAsync_WhenPointsArePositive_CreatesStudentPoints()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.AddAssignmentPointsAsync(studentId, 30, CancellationToken.None);

        Assert.Equal(30, db.StudentPoints.Local.Single().TotalPoints);
    }

    // -----------------------------------------------------------------------
    // AddChallengePointsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddChallengePointsAsync_WhenPointsAreZero_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.AddChallengePointsAsync(Guid.NewGuid(), 0, CancellationToken.None);

        Assert.Empty(db.StudentPoints.Local);
    }

    [Fact]
    public async Task AddChallengePointsAsync_WhenRecordExists_IncreasesTotalPoints()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = studentId, TotalPoints = 200 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.AddChallengePointsAsync(studentId, 50, CancellationToken.None);

        Assert.Equal(250, db.StudentPoints.Local.Single().TotalPoints);
    }

    // -----------------------------------------------------------------------
    // UpdateStreakAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateStreakAsync_WhenNoStreak_CreatesNewStreak()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var studentId = Guid.NewGuid();

        await service.UpdateStreakAsync(studentId, CancellationToken.None);

        var streak = db.StudentStreaks.Local.Single();
        Assert.Equal(studentId, streak.StudentProfileId);
        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(1, streak.BestStreak);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), streak.LastActiveDate);
    }

    [Fact]
    public async Task UpdateStreakAsync_WhenAlreadyActiveToday_DoesNotModifyStreak()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = studentId,
            CurrentStreak = 3,
            BestStreak = 5,
            LastActiveDate = today
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateStreakAsync(studentId, CancellationToken.None);

        var streak = db.StudentStreaks.Local.Single();
        Assert.Equal(3, streak.CurrentStreak);
        Assert.Equal(5, streak.BestStreak);
    }

    [Fact]
    public async Task UpdateStreakAsync_WhenConsecutiveDay_IncrementsAndUpdatesBestStreak()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = studentId,
            CurrentStreak = 1,
            BestStreak = 1,
            LastActiveDate = yesterday
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateStreakAsync(studentId, CancellationToken.None);

        var streak = db.StudentStreaks.Local.Single();
        Assert.Equal(2, streak.CurrentStreak);
        Assert.Equal(2, streak.BestStreak);
    }

    [Fact]
    public async Task UpdateStreakAsync_WhenConsecutiveDay_IncrementsButKeepsBestStreak()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = studentId,
            CurrentStreak = 2,
            BestStreak = 5,
            LastActiveDate = yesterday
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateStreakAsync(studentId, CancellationToken.None);

        var streak = db.StudentStreaks.Local.Single();
        Assert.Equal(3, streak.CurrentStreak);
        Assert.Equal(5, streak.BestStreak);
    }

    [Fact]
    public async Task UpdateStreakAsync_WhenStreakBrokenAndOldStreakGreaterThanOne_PublishesEvent()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var oldDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = studentId,
            CurrentStreak = 4,
            BestStreak = 5,
            LastActiveDate = oldDate
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateStreakAsync(studentId, CancellationToken.None);

        var streak = db.StudentStreaks.Local.Single();
        Assert.Equal(1, streak.CurrentStreak);
        Assert.Equal(5, streak.BestStreak);
        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<StreakBrokenEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStreakAsync_WhenStreakBrokenAndOldStreakIsOne_DoesNotPublishEvent()
    {
        await using var db = CreateDbContext();
        var studentId = Guid.NewGuid();
        var oldDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10);
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = studentId,
            CurrentStreak = 1,
            BestStreak = 3,
            LastActiveDate = oldDate
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateStreakAsync(studentId, CancellationToken.None);

        var streak = db.StudentStreaks.Local.Single();
        Assert.Equal(1, streak.CurrentStreak);
        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<StreakBrokenEvent>(), CancellationToken.None),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // GetLeaderboardAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetLeaderboardAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetLeaderboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetLeaderboardAsync_WhenClassFound_ReturnsLeaderboardOrderedByPoints()
    {
        await using var db = CreateDbContext();
        var cls = new Class
        {
            OrganizationId = OrgId,
            TeacherId = TeacherId,
            Name = "3A",
            Grade = 3,
            SchoolYear = "2024/2025"
        };
        db.Classes.Add(cls);
        var student1 = new StudentProfile { ClassId = cls.Id, FirstName = "Alice", LastName = "Smith" };
        var student2 = new StudentProfile { ClassId = cls.Id, FirstName = "Bob", LastName = "Jones" };
        db.Students.Add(student1);
        db.Students.Add(student2);
        await db.SaveChangesAsync();

        db.StudentPoints.Add(new StudentPoints
        {
            StudentProfileId = student1.Id,
            StudentProfile = student1,
            TotalPoints = 100
        });
        db.StudentPoints.Add(new StudentPoints
        {
            StudentProfileId = student2.Id,
            StudentProfile = student2,
            TotalPoints = 200
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetLeaderboardAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Bob Jones", result.Data[0].StudentName);
        Assert.Equal(200, result.Data[0].Points);
        Assert.Equal("Alice Smith", result.Data[1].StudentName);
        Assert.Equal(100, result.Data[1].Points);
    }
}
