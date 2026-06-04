using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class StudentServiceGamificationSummaryTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid StudentUserId = new("33333333-3333-3333-3333-333333333333");

    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();

    public StudentServiceGamificationSummaryTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(StudentUserId);
        _currentUserMock.Setup(u => u.OrganizationId).Returns(OrgId);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private StudentService CreateService(AppDbContext db)
        => new(db, _currentUserMock.Object, _eventDispatcherMock.Object);

    private static Class SeedClass(AppDbContext db)
    {
        var cls = new Class
        {
            TeacherId = TeacherId,
            OrganizationId = OrgId,
            Name = "3A",
            Grade = 3,
            SchoolYear = "2025/2026"
        };
        db.Classes.Add(cls);
        return cls;
    }

    private static StudentProfile SeedStudent(
        AppDbContext db,
        Guid? classId,
        Guid? userId = null,
        bool isActive = true)
    {
        var student = new StudentProfile
        {
            ClassId = classId,
            FirstName = "Student",
            LastName = "Test",
            UserId = userId,
            IsActive = isActive
        };
        db.Students.Add(student);
        return student;
    }

    private static void SeedPoints(AppDbContext db, Guid studentId, int totalPoints)
    {
        db.StudentPoints.Add(new StudentPoints
        {
            StudentProfileId = studentId,
            TotalPoints = totalPoints
        });
    }

    private static void SeedStreak(AppDbContext db, Guid studentId, int current, int best)
    {
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = studentId,
            CurrentStreak = current,
            BestStreak = best
        });
    }

    private static Badge SeedBadge(AppDbContext db, string code, string name, string icon)
    {
        var badge = new Badge
        {
            Code = code,
            Name = name,
            Icon = icon,
            Description = "desc"
        };
        db.Badges.Add(badge);
        return badge;
    }

    private static void SeedStudentBadge(
        AppDbContext db, Guid studentId, Guid badgeId, DateTime awardedAt)
    {
        db.StudentBadges.Add(new StudentBadge
        {
            StudentProfileId = studentId,
            BadgeId = badgeId,
            AwardedAt = awardedAt
        });
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenStudentProfileNotFound_ReturnsFail()
    {
        using var db = CreateDbContext();
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenStudentNotEnrolled_ReturnsFail()
    {
        using var db = CreateDbContext();
        SeedStudent(db, classId: null, userId: StudentUserId);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student is not enrolled in a class.", result.Error);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenNoPointsRecord_ReturnsZeroPoints()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        SeedStudent(db, cls.Id, StudentUserId);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.Data!.TotalPoints);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenNoStreakRecord_ReturnsZeroStreaks()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        var student = SeedStudent(db, cls.Id, StudentUserId);
        await db.SaveChangesAsync(CancellationToken.None);
        SeedPoints(db, student.Id, 50);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(50, result.Data!.TotalPoints);
        Assert.Equal(0, result.Data.CurrentStreak);
        Assert.Equal(0, result.Data.BestStreak);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenStreakExists_ReturnsStreakValues()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        var student = SeedStudent(db, cls.Id, StudentUserId);
        await db.SaveChangesAsync(CancellationToken.None);
        SeedStreak(db, student.Id, current: 4, best: 9);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.CurrentStreak);
        Assert.Equal(9, result.Data.BestStreak);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenNoBadges_ReturnsNullRecentBadgeAndZeroCount()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        var student = SeedStudent(db, cls.Id, StudentUserId);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Null(result.Data!.RecentBadge);
        Assert.Equal(0, result.Data.TotalBadges);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenBadgesExist_ReturnsMostRecentBadgeAndCount()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        var student = SeedStudent(db, cls.Id, StudentUserId);
        var oldBadge = SeedBadge(db, "streak_3", "Three Day Streak", "fire");
        var newBadge = SeedBadge(db, "streak_7", "Seven Day Streak", "star");
        await db.SaveChangesAsync(CancellationToken.None);
        SeedStudentBadge(db, student.Id, oldBadge.Id, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SeedStudentBadge(db, student.Id, newBadge.Id, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.TotalBadges);
        Assert.NotNull(result.Data.RecentBadge);
        Assert.Equal("streak_7", result.Data.RecentBadge!.Code);
        Assert.Equal("Seven Day Streak", result.Data.RecentBadge.Name);
        Assert.Equal("star", result.Data.RecentBadge.Icon);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenStudentIsTopInClass_ReturnsRankOne()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        var student = SeedStudent(db, cls.Id, StudentUserId);
        var other = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync(CancellationToken.None);
        SeedPoints(db, student.Id, 100);
        SeedPoints(db, other.Id, 50);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data!.ClassRank);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenOthersHaveMorePoints_ReturnsHigherRank()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        var student = SeedStudent(db, cls.Id, StudentUserId);
        var other1 = SeedStudent(db, cls.Id);
        var other2 = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync(CancellationToken.None);
        SeedPoints(db, student.Id, 20);
        SeedPoints(db, other1.Id, 80);
        SeedPoints(db, other2.Id, 60);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, result.Data!.ClassRank);
    }

    [Fact]
    public async Task GetGamificationSummaryAsync_WhenClassHasStudents_ReturnsClassSizeIncludingInactive()
    {
        using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync(CancellationToken.None);
        SeedStudent(db, cls.Id, StudentUserId);
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id, isActive: false);
        await db.SaveChangesAsync(CancellationToken.None);
        var service = CreateService(db);

        var result = await service.GetGamificationSummaryAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(4, result.Data!.ClassSize);
    }
}
