using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Challenges;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class ChallengeServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ILearningActivityService> _learningActivityMock = new();
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();

    public ChallengeServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
        _learningActivityMock
            .Setup(x => x.CreateForChallengeAsync(It.IsAny<Challenge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private ChallengeService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object, _learningActivityMock.Object, _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static Class SeedClass(AppDbContext db)
    {
        var cls = new Class
        {
            OrganizationId = OrgId,
            TeacherId = TeacherId,
            Name = "3A",
            Grade = 3,
            SchoolYear = "2024/2025"
        };
        db.Classes.Add(cls);
        return cls;
    }

    private static StudentProfile SeedStudent(
        AppDbContext db, Guid classId, string firstName, string lastName)
    {
        var student = new StudentProfile
        {
            ClassId = classId,
            FirstName = firstName,
            LastName = lastName
        };
        db.Students.Add(student);
        return student;
    }

    private static Challenge SeedChallenge(AppDbContext db, Class cls, DateTime? endDate = null)
    {
        var challenge = new Challenge
        {
            ClassId = cls.Id,
            Class = cls,
            Title = "Read 5 Books",
            Description = "Challenge description",
            TargetType = TargetType.Books,
            TargetValue = 5,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = endDate ?? DateTime.UtcNow.AddDays(30),
            Points = 100
        };
        db.Challenges.Add(challenge);
        return challenge;
    }

    private static ChallengeProgress SeedProgress(
        AppDbContext db,
        Challenge challenge,
        StudentProfile student,
        bool completed = false,
        DateTime? startedAt = null)
    {
        var progress = new ChallengeProgress
        {
            ChallengeId = challenge.Id,
            Challenge = challenge,
            StudentProfileId = student.Id,
            StudentProfile = student,
            CurrentValue = 0,
            Completed = completed,
            StartedAt = startedAt
        };
        db.ChallengeProgress.Add(progress);
        return progress;
    }

    // -----------------------------------------------------------------------
    // CreateChallengeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateChallengeAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateChallengeAsync(
            Guid.NewGuid(),
            new ChallengeCreateRequest { Title = "T", Description = "D", TargetValue = 5, Points = 10 },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task CreateChallengeAsync_WhenClassFoundWithNoStudents_ReturnsOkAndDispatchesEvent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CreateChallengeAsync(cls.Id, new ChallengeCreateRequest
        {
            Title = "Read 5 Books",
            Description = "Read",
            TargetType = TargetType.Books,
            TargetValue = 5,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            Points = 100
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, db.Challenges.Count());
        Assert.Equal(0, db.ChallengeProgress.Count());
        _learningActivityMock.Verify(
            x => x.CreateForChallengeAsync(It.IsAny<Challenge>(), CancellationToken.None),
            Times.Once);
        _eventDispatcherMock.Verify(
            x => x.PublishAsync(It.IsAny<ChallengeCreatedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CreateChallengeAsync_WhenClassFoundWithStudents_CreatesChallengeProgressPerStudent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        SeedStudent(db, cls.Id, "Alice", "Smith");
        SeedStudent(db, cls.Id, "Bob", "Jones");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CreateChallengeAsync(cls.Id, new ChallengeCreateRequest
        {
            Title = "T",
            Description = "D",
            TargetValue = 5,
            Points = 10,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, db.ChallengeProgress.Count());
        Assert.All(db.ChallengeProgress, p => Assert.Equal(0, p.CurrentValue));
    }

    // -----------------------------------------------------------------------
    // GetChallengesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetChallengesAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetChallengesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetChallengesAsync_WhenClassFound_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetChallengesAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetChallengesAsync_WhenClassFound_ReturnsChallengeDtosWithCorrectCounts()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student1 = SeedStudent(db, cls.Id, "Alice", "Smith");
        var student2 = SeedStudent(db, cls.Id, "Bob", "Jones");
        await db.SaveChangesAsync();

        var challenge = SeedChallenge(db, cls);
        await db.SaveChangesAsync();

        SeedProgress(db, challenge, student1, completed: true, startedAt: DateTime.UtcNow);
        SeedProgress(db, challenge, student2, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetChallengesAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        var dto = result.Data[0];
        Assert.Equal("Read 5 Books", dto.Title);
        Assert.Equal(2, dto.TotalStudents);
        Assert.Equal(1, dto.CompletedCount);
        Assert.False(dto.IsExpired);
    }

    [Fact]
    public async Task GetChallengesAsync_WhenEndDateInPast_IsExpiredTrue()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();

        SeedChallenge(db, cls, endDate: DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetChallengesAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data[0].IsExpired);
    }

    // -----------------------------------------------------------------------
    // ExtendChallengeDeadlineAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ExtendChallengeDeadlineAsync_WhenChallengeNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.ExtendChallengeDeadlineAsync(
            cls.Id,
            Guid.NewGuid(),
            new ExtendChallengeDeadlineRequest { EndDate = DateTime.UtcNow.AddDays(60) },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Challenge not found.", result.Error);
    }

    [Fact]
    public async Task ExtendChallengeDeadlineAsync_WhenChallengeFound_UpdatesEndDateAndReturnsOk()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db, cls);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var newEndDate = DateTime.UtcNow.AddDays(60);
        var result = await service.ExtendChallengeDeadlineAsync(
            cls.Id,
            challenge.Id,
            new ExtendChallengeDeadlineRequest { EndDate = newEndDate },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal(newEndDate, db.Challenges.Find(challenge.Id)!.EndDate);
    }

    // -----------------------------------------------------------------------
    // GetStudentProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStudentProgressAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetStudentProgressAsync_WhenClassFound_ReturnsProgressDtosOrderedByName()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student1 = SeedStudent(db, cls.Id, "Zack", "Adams");
        var student2 = SeedStudent(db, cls.Id, "Alice", "Smith");
        await db.SaveChangesAsync();

        var challenge = SeedChallenge(db, cls);
        await db.SaveChangesAsync();

        SeedProgress(db, challenge, student2, completed: true, startedAt: DateTime.UtcNow);
        SeedProgress(db, challenge, student1, completed: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetStudentProgressAsync(cls.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Alice Smith", result.Data[0].StudentName);
        Assert.True(result.Data[0].Completed);
        Assert.True(result.Data[0].Started);
        Assert.Equal("Zack Adams", result.Data[1].StudentName);
        Assert.False(result.Data[1].Completed);
        Assert.False(result.Data[1].Started);
    }
}
