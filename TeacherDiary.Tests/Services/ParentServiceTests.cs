using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class ParentServiceTests
{
    private static readonly Guid MyId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public ParentServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(MyId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (AppDbContext db, SqliteConnection conn) CreateSqliteDbContext()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(conn)
                .Options);
        db.Database.EnsureCreated();
        return (db, conn);
    }

    private ParentService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object);

private static StudentProfile SeedStudent(
        AppDbContext db,
        Guid? parentId = null,
        Guid? classId = null,
        string firstName = "Alice",
        string lastName = "Smith")
    {
        var s = new StudentProfile
        {
            ParentId = parentId,
            ClassId = classId,
            FirstName = firstName,
            LastName = lastName
        };
        db.Students.Add(s);
        return s;
    }

    private static Challenge SeedChallenge(AppDbContext db, int targetValue = 10, int daysUntilEnd = 7)
    {
        var c = new Challenge
        {
            ClassId = Guid.NewGuid(),
            Title = "Challenge",
            Description = "Desc",
            TargetValue = targetValue,
            EndDate = DateTime.UtcNow.AddDays(daysUntilEnd)
        };
        db.Challenges.Add(c);
        return c;
    }

[Fact]
    public async Task CreateStudentAsync_WhenNotAuthenticated_ReturnsFail()
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateStudentAsync(
            new CreateStudentRequest { FirstName = "Alice", LastName = "Smith" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Unauthorized", result.Error);
    }

    [Fact]
    public async Task CreateStudentAsync_WhenAuthenticated_PersistsStudentAndReturnsId()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateStudentAsync(
            new CreateStudentRequest { FirstName = "Bob", LastName = "Jones" },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);
        var s = db.Students.Local.Single();
        Assert.Equal(MyId, s.ParentId);
        Assert.Equal("Bob", s.FirstName);
        Assert.Equal("Jones", s.LastName);
        Assert.Null(s.ClassId);
    }

[Fact]
    public async Task GetMyStudentsAsync_WhenStudentHasStreakAndPoints_SetsBothMedalCodes()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = student.Id,
            CurrentStreak = 10,
            BestStreak = 10
        });
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = student.Id,
            ActivityType = ActivityType.ReadingProgress,
            ReferenceType = ActivityReferenceType.AssignedBook,
            ReferenceId = Guid.NewGuid(),
            PointsEarned = 500
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyStudentsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.NotNull(result.Data[0].TopMedalCode);
        Assert.NotNull(result.Data[0].TopPointsMedalCode);
    }

    [Fact]
    public async Task GetMyStudentsAsync_WhenStudentHasNoStreakOrPoints_MedalCodesAreNull()
    {
        await using var db = CreateDbContext();
        SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyStudentsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Null(result.Data[0].TopMedalCode);
        Assert.Null(result.Data[0].TopPointsMedalCode);
    }

[Fact]
    public async Task GetStudentAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetStudentAsync_WhenStudentHasNoActivityLogs_StatsAreZero()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        var dto = result.Data;
        Assert.Equal(0, dto.TotalPagesRead);
        Assert.Equal(0, dto.CompletedAssignments);
        Assert.Equal(0, dto.TotalPoints);
        Assert.Empty(dto.ActivityLast7Days);
    }

    [Fact]
    public async Task GetStudentAsync_WhenStudentHasAllActivityTypes_MapsAllSwitchDescriptions()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var entries = new (ActivityType type, int? pages)[]
        {
            (ActivityType.ReadingProgress, 10),
            (ActivityType.ReadingProgress, null),
            (ActivityType.AssignmentCompleted, null),
            (ActivityType.AssignmentStarted, null),
            (ActivityType.ChallengeCompleted, null),
            (ActivityType.ChallengeProgressUpdated, null),
            (ActivityType.LearningActivityCompleted,null),
            (ActivityType.LearningActivityStarted, null),
            ((ActivityType)99, null),
        };

        foreach (var (type, pages) in entries)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                StudentProfileId = student.Id,
                ActivityType = type,
                ReferenceType = ActivityReferenceType.AssignedBook,
                ReferenceId = Guid.NewGuid(),
                Date = today,
                PagesRead = pages,
                PointsEarned = 10
            });
        }
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        var dto = result.Data;
        Assert.NotEqual(0, dto.TotalPoints);

        var descs = dto.ActivityLast7Days.Select(a => a.Description).ToList();
        Assert.Contains("Прочел 10 стр.", descs);
        Assert.Contains("Прочел 0 стр.", descs);
        Assert.Contains("Завърши задача", descs);
        Assert.Contains("Стартира задача", descs);
        Assert.Contains("Завърши предизвикателство", descs);
        Assert.Contains("Актуализира предизвикателство", descs);
        Assert.Contains("Завърши учебна дейност", descs);
        Assert.Contains("Стартира учебна дейност", descs);
        Assert.Contains("Активност", descs);
    }

    [Fact]
    public async Task GetStudentAsync_WhenReadingProgressExists_MapsIsExpiredCorrectly()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var book = new Book { Title = "Book", Author = "Author" };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var expiredAb = new AssignedBook
        {
            ClassId = Guid.NewGuid(), BookId = book.Id, Book = book,
            EndDateUtc = DateTime.UtcNow.AddDays(-1)
        };
        var openAb = new AssignedBook
        {
            ClassId = Guid.NewGuid(), BookId = book.Id, Book = book,
            EndDateUtc = null
        };
        db.AssignedBooks.Add(expiredAb);
        db.AssignedBooks.Add(openAb);
        await db.SaveChangesAsync();

        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId = expiredAb.Id,
            AssignedBook = expiredAb
        });
        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId = openAb.Id,
            AssignedBook = openAb
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Reading, r => r.IsExpired);
        Assert.Contains(result.Data.Reading, r => !r.IsExpired);
    }

    [Fact]
    public async Task GetStudentAsync_WhenAssignmentProgressExists_MapsIsExpiredCorrectly()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var pastAssignment = new Assignment
        {
            ClassId = Guid.NewGuid(), CreatedByTeacherId = Guid.NewGuid(),
            Title = "Past", Description = "d",
            DueDate = DateTime.UtcNow.AddDays(-1)
        };
        var openAssignment = new Assignment
        {
            ClassId = Guid.NewGuid(), CreatedByTeacherId = Guid.NewGuid(),
            Title = "Open", Description = "d",
            DueDate = null
        };
        db.Assignments.Add(pastAssignment);
        db.Assignments.Add(openAssignment);
        await db.SaveChangesAsync();

        db.AssignmentProgress.Add(new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId = pastAssignment.Id,
            Assignment = pastAssignment
        });
        db.AssignmentProgress.Add(new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId = openAssignment.Id,
            Assignment = openAssignment
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Assignments, a => a.IsExpired);
        Assert.Contains(result.Data.Assignments, a => !a.IsExpired);
    }

    [Fact]
    public async Task GetStudentAsync_WhenLearningActivitiesExist_MapsIsExpiredCorrectly()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var expiredLa = new LearningActivity
        {
            ClassId = Guid.NewGuid(), CreatedByTeacherId = Guid.NewGuid(),
            Title = "Expired", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            DueDateUtc = DateTime.UtcNow.AddDays(-1)
        };
        var openLa = new LearningActivity
        {
            ClassId = Guid.NewGuid(), CreatedByTeacherId = Guid.NewGuid(),
            Title = "Open", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            DueDateUtc = null
        };
        db.LearningActivities.Add(expiredLa);
        db.LearningActivities.Add(openLa);
        await db.SaveChangesAsync();

        db.StudentLearningActivityProgress.Add(new StudentLearningActivityProgress
        {
            StudentProfileId = student.Id,
            LearningActivityId = expiredLa.Id,
            LearningActivity = expiredLa
        });
        db.StudentLearningActivityProgress.Add(new StudentLearningActivityProgress
        {
            StudentProfileId = student.Id,
            LearningActivityId = openLa.Id,
            LearningActivity = openLa
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.LearningActivities, la => la.IsExpired);
        Assert.Contains(result.Data.LearningActivities, la => !la.IsExpired);
    }

    [Fact]
    public async Task GetStudentAsync_WhenChallengeProgressExists_MapsStartedAndIsExpired()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var pastChallenge = new Challenge
        {
            ClassId = Guid.NewGuid(), Title = "C", Description = "D",
            TargetValue = 10, EndDate = DateTime.UtcNow.AddDays(-1)
        };
        db.Challenges.Add(pastChallenge);
        await db.SaveChangesAsync();

        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = pastChallenge.Id,
            Challenge = pastChallenge,
            StartedAt = DateTime.UtcNow
        });
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = pastChallenge.Id,
            Challenge = pastChallenge,
            StartedAt = null
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Challenges, c => c.Started);
        Assert.Contains(result.Data.Challenges, c => !c.Started);
        Assert.All(result.Data.Challenges, c => Assert.True(c.IsExpired));
    }

[Fact]
    public async Task StartChallengeForStudentAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.StartChallengeForStudentAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student not found.", result.Error);
    }

    [Fact]
    public async Task StartChallengeForStudentAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeForStudentAsync(
            student.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Challenge not found.", result.Error);
    }

    [Fact]
    public async Task StartChallengeForStudentAsync_WhenAlreadyStarted_ReturnsOkWithoutSaving()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db);
        var progress = new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = challenge.Id,
            StartedAt = DateTime.UtcNow.AddDays(-1)
        };
        db.ChallengeProgress.Add(progress);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.NotNull(db.ChallengeProgress.Local.Single().StartedAt);
    }

    [Fact]
    public async Task StartChallengeForStudentAsync_WhenNotYetStarted_SetsStartedAt()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db);
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = challenge.Id,
            StartedAt = null
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(db.ChallengeProgress.Local.Single().StartedAt);
    }

[Fact]
    public async Task CompleteChallengeForStudentAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student not found.", result.Error);
    }

    [Fact]
    public async Task CompleteChallengeForStudentAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            student.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Challenge not found.", result.Error);
    }

    [Fact]
    public async Task CompleteChallengeForStudentAsync_WhenAlreadyCompleted_ReturnsOkWithoutSaving()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db, targetValue: 5);
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = challenge.Id,
            Challenge = challenge,
            Completed = true
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task CompleteChallengeForStudentAsync_WhenNotCompletedAndTargetPositive_UpdatesCurrentValue()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db, targetValue: 10);
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = challenge.Id,
            Challenge = challenge,
            Completed = false,
            CurrentValue = 3
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        var saved = db.ChallengeProgress.Local.Single();
        Assert.True(saved.Completed);
        Assert.NotNull(saved.CompletedAt);
        Assert.Equal(10, saved.CurrentValue);
    }

    [Fact]
    public async Task CompleteChallengeForStudentAsync_WhenNotCompletedAndTargetZero_KeepsCurrentValue()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db, targetValue: 0);
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = challenge.Id,
            Challenge = challenge,
            Completed = false,
            CurrentValue = 5
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        var saved = db.ChallengeProgress.Local.Single();
        Assert.True(saved.Completed);
        Assert.Equal(5, saved.CurrentValue);
    }

[Fact]
    public async Task DeleteStudentAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.DeleteStudentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student not found.", result.Error);
    }

    [Fact]
    public async Task DeleteStudentAsync_WhenStudentIsInClass_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId, classId: Guid.NewGuid());
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.DeleteStudentAsync(student.Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Детето е записано в клас", result.Error);
    }

    [Fact]
    public async Task DeleteStudentAsync_WhenStudentNotInClass_RemovesStudentAndReturnsOk()
    {
        var (db, conn) = CreateSqliteDbContext();
        await using (db)
        await using (conn)
        {
            db.Users.Add(new AppUser { Id = MyId, UserName = "parent@test.com" });
            await db.SaveChangesAsync();

            var student = SeedStudent(db, parentId: MyId, classId: null);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.DeleteStudentAsync(student.Id, CancellationToken.None);

            Assert.True(result.Success);
            Assert.True(result.Data);
            Assert.Equal(0, await db.Students.CountAsync());
        }
    }

[Fact]
    public async Task GetStudentActivityCalendarAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentActivityCalendarAsync(Guid.NewGuid(), 30, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student not found.", result.Error);
    }

    [Fact]
    public async Task GetStudentActivityCalendarAsync_WhenDaysZero_DefaultsTo30()
    {
        await using var db = CreateDbContext();
        var s = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentActivityCalendarAsync(s.Id, 0, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(30, result.Data.Count);
    }

    [Fact]
    public async Task GetStudentActivityCalendarAsync_WhenDaysOver90_CapsAt90()
    {
        await using var db = CreateDbContext();
        var s = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentActivityCalendarAsync(s.Id, 200, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(90, result.Data.Count);
    }

    [Fact]
    public async Task GetStudentActivityCalendarAsync_WhenNoLogs_AllDaysHaveNoActivity()
    {
        await using var db = CreateDbContext();
        var s = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentActivityCalendarAsync(s.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(7, result.Data.Count);
        Assert.All(result.Data, d =>
        {
            Assert.False(d.HasActivity);
            Assert.Equal(0, d.PointsEarned);
            Assert.Equal(0, d.PagesRead);
            Assert.Equal(0, d.ActivityCount);
        });
    }

    [Fact]
    public async Task GetStudentActivityCalendarAsync_WhenLogsExist_MapsCorrectly()
    {
        await using var db = CreateDbContext();
        var s = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = s.Id,
            ActivityType = ActivityType.ReadingProgress,
            ReferenceType = ActivityReferenceType.AssignedBook,
            ReferenceId = Guid.NewGuid(),
            Date = today,
            PagesRead = 15,
            PointsEarned = 30
        });
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = s.Id,
            ActivityType = ActivityType.AssignmentCompleted,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = Guid.NewGuid(),
            Date = today,
            PagesRead = null,
            PointsEarned = 20
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentActivityCalendarAsync(s.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var todayEntry = result.Data.Single(d => d.Date == today);
        Assert.True(todayEntry.HasActivity);
        Assert.Equal(50, todayEntry.PointsEarned);
        Assert.Equal(15, todayEntry.PagesRead);
        Assert.Equal(2, todayEntry.ActivityCount);
    }

[Fact]
    public async Task GetStudentLeaderboardAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentLeaderboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student not found.", result.Error);
    }

    [Fact]
    public async Task GetStudentLeaderboardAsync_WhenStudentHasNoClass_ReturnsEmpty()
    {
        await using var db = CreateDbContext();
        var s = SeedStudent(db, parentId: MyId, classId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentLeaderboardAsync(s.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetStudentLeaderboardAsync_WhenClassHasStudents_ReturnsOrderedByPoints()
    {
        await using var db = CreateDbContext();
        var classId = Guid.NewGuid();

        var child = SeedStudent(db, parentId: MyId, classId: classId, firstName: "Alice", lastName: "Smith");
        var mate = SeedStudent(db, parentId: null, classId: classId, firstName: "Bob", lastName: "Jones");
        await db.SaveChangesAsync();

        db.StudentPoints.Add(new StudentPoints { StudentProfileId = child.Id, TotalPoints = 100 });
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = mate.Id, TotalPoints = 200 });
        db.StudentStreaks.Add(new StudentStreak { StudentProfileId = child.Id, CurrentStreak = 5, BestStreak = 5, LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentLeaderboardAsync(child.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(mate.Id, result.Data[0].StudentId);
        Assert.Equal(200, result.Data[0].Points);
        Assert.Equal(child.Id, result.Data[1].StudentId);
        Assert.Equal(100, result.Data[1].Points);
        Assert.Null(result.Data[0].TopMedalCode);
        Assert.Equal(BadgeCodes.Streak5, result.Data[1].TopMedalCode);
    }

    [Fact]
    public async Task GetStudentLeaderboardAsync_WhenStudentHasNoPointsRecord_ShowsZeroPoints()
    {
        await using var db = CreateDbContext();
        var classId = Guid.NewGuid();
        var s = SeedStudent(db, parentId: MyId, classId: classId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentLeaderboardAsync(s.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal(0, result.Data[0].Points);
    }
}
