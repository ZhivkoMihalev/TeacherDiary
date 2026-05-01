using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class ParentServiceTests
{
    private static readonly Guid MyId    = new("11111111-1111-1111-1111-111111111111");
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

    // SQLite in-memory required for ExecuteDeleteAsync (not supported by InMemory provider).
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

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

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

    // -----------------------------------------------------------------------
    // CreateStudentAsync
    // -----------------------------------------------------------------------

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

    // -----------------------------------------------------------------------
    // GetMyStudentsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMyStudentsAsync_WhenStudentHasStreakAndPoints_SetsBothMedalCodes()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        // BestStreak = 10 → GetStreakMedalCode(10) returns SevenDayStreak (non-null)
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = student.Id,
            CurrentStreak = 10,
            BestStreak = 10
        });
        // PointsEarned = 500 → sum 500 → GetPointsMedalCode(500) returns Points500 (non-null)
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
        Assert.NotNull(result.Data[0].TopMedalCode);       // TryGetValue streak → true
        Assert.NotNull(result.Data[0].TopPointsMedalCode); // TryGetValue points → true
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
        Assert.Null(result.Data[0].TopMedalCode);          // TryGetValue streak → false
        Assert.Null(result.Data[0].TopPointsMedalCode);    // TryGetValue points → false
    }

    // -----------------------------------------------------------------------
    // GetStudentAsync
    // -----------------------------------------------------------------------

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
        Assert.Equal(0, dto.TotalPagesRead);         // stats = null → ?? 0 (null branch)
        Assert.Equal(0, dto.CompletedAssignments);   // stats = null → ?? 0 (null branch)
        Assert.Equal(0, dto.TotalPoints);            // stats = null → ?? 0 (null branch)
        Assert.Empty(dto.ActivityLast7Days);
    }

    [Fact]
    public async Task GetStudentAsync_WhenStudentHasAllActivityTypes_MapsAllSwitchDescriptions()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Seed one log for each ActivityType (within last 7 days) + default arm via cast
        var entries = new (ActivityType type, int? pages)[]
        {
            (ActivityType.ReadingProgress,            10),  // PagesRead non-null → uses value
            (ActivityType.ReadingProgress,          null),  // PagesRead = null  → ?? 0 (null branch)
            (ActivityType.AssignmentCompleted,      null),
            (ActivityType.AssignmentStarted,        null),
            (ActivityType.ChallengeCompleted,       null),
            (ActivityType.ChallengeProgressUpdated, null),
            (ActivityType.LearningActivityCompleted,null),
            (ActivityType.LearningActivityStarted,  null),
            ((ActivityType)99,                      null),  // default switch arm
        };

        foreach (var (type, pages) in entries)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                StudentProfileId = student.Id,
                ActivityType     = type,
                ReferenceType    = ActivityReferenceType.AssignedBook,
                ReferenceId      = Guid.NewGuid(),
                Date             = today,
                PagesRead        = pages,
                PointsEarned     = 10
            });
        }
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        var dto = result.Data;
        Assert.NotEqual(0, dto.TotalPoints);  // stats non-null → ?? 0 (non-null branch)

        var descs = dto.ActivityLast7Days.Select(a => a.Description).ToList();
        Assert.Contains("Прочел 10 стр.",            descs);  // PagesRead non-null
        Assert.Contains("Прочел 0 стр.",             descs);  // PagesRead null → ?? 0
        Assert.Contains("Завърши задача",            descs);
        Assert.Contains("Стартира задача",           descs);
        Assert.Contains("Завърши предизвикателство", descs);
        Assert.Contains("Актуализира предизвикателство", descs);
        Assert.Contains("Завърши учебна дейност",   descs);
        Assert.Contains("Стартира учебна дейност",  descs);
        Assert.Contains("Активност",                descs);   // default arm
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
            EndDateUtc = DateTime.UtcNow.AddDays(-1)   // past → IsExpired = true
        };
        var openAb = new AssignedBook
        {
            ClassId = Guid.NewGuid(), BookId = book.Id, Book = book,
            EndDateUtc = null                           // null → IsExpired = false
        };
        db.AssignedBooks.Add(expiredAb);
        db.AssignedBooks.Add(openAb);
        await db.SaveChangesAsync();

        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = expiredAb.Id,
            AssignedBook     = expiredAb
        });
        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = openAb.Id,
            AssignedBook     = openAb
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Reading, r => r.IsExpired);    // EndDateUtc past → true
        Assert.Contains(result.Data.Reading, r => !r.IsExpired);   // EndDateUtc null → false
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
            DueDate = DateTime.UtcNow.AddDays(-1)  // past → IsExpired = true
        };
        var openAssignment = new Assignment
        {
            ClassId = Guid.NewGuid(), CreatedByTeacherId = Guid.NewGuid(),
            Title = "Open", Description = "d",
            DueDate = null                         // null → IsExpired = false
        };
        db.Assignments.Add(pastAssignment);
        db.Assignments.Add(openAssignment);
        await db.SaveChangesAsync();

        db.AssignmentProgress.Add(new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId     = pastAssignment.Id,
            Assignment       = pastAssignment
        });
        db.AssignmentProgress.Add(new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId     = openAssignment.Id,
            Assignment       = openAssignment
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Assignments, a => a.IsExpired);    // DueDate past → true
        Assert.Contains(result.Data.Assignments, a => !a.IsExpired);   // DueDate null → false
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
            DueDateUtc = DateTime.UtcNow.AddDays(-1)   // past → IsExpired = true
        };
        var openLa = new LearningActivity
        {
            ClassId = Guid.NewGuid(), CreatedByTeacherId = Guid.NewGuid(),
            Title = "Open", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            DueDateUtc = null                           // null → IsExpired = false
        };
        db.LearningActivities.Add(expiredLa);
        db.LearningActivities.Add(openLa);
        await db.SaveChangesAsync();

        db.StudentLearningActivityProgress.Add(new StudentLearningActivityProgress
        {
            StudentProfileId   = student.Id,
            LearningActivityId = expiredLa.Id,
            LearningActivity   = expiredLa
        });
        db.StudentLearningActivityProgress.Add(new StudentLearningActivityProgress
        {
            StudentProfileId   = student.Id,
            LearningActivityId = openLa.Id,
            LearningActivity   = openLa
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.LearningActivities, la => la.IsExpired);    // DueDateUtc past → true
        Assert.Contains(result.Data.LearningActivities, la => !la.IsExpired);   // DueDateUtc null → false
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
            TargetValue = 10, EndDate = DateTime.UtcNow.AddDays(-1)  // past → IsExpired = true
        };
        db.Challenges.Add(pastChallenge);
        await db.SaveChangesAsync();

        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId      = pastChallenge.Id,
            Challenge        = pastChallenge,
            StartedAt        = DateTime.UtcNow   // Started = true (StartedAt != null)
        });
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId      = pastChallenge.Id,
            Challenge        = pastChallenge,
            StartedAt        = null              // Started = false (StartedAt == null)
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Challenges, c => c.Started);    // StartedAt != null → true
        Assert.Contains(result.Data.Challenges, c => !c.Started);   // StartedAt == null → false
        Assert.All(result.Data.Challenges, c => Assert.True(c.IsExpired)); // EndDate past
    }

    // -----------------------------------------------------------------------
    // StartChallengeForStudentAsync
    // -----------------------------------------------------------------------

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
            ChallengeId      = challenge.Id,
            StartedAt        = DateTime.UtcNow.AddDays(-1)  // already started
        };
        db.ChallengeProgress.Add(progress);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
        // StartedAt unchanged (early return before SaveChanges)
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
            ChallengeId      = challenge.Id,
            StartedAt        = null
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(db.ChallengeProgress.Local.Single().StartedAt);
    }

    // -----------------------------------------------------------------------
    // CompleteChallengeForStudentAsync
    // -----------------------------------------------------------------------

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
            ChallengeId      = challenge.Id,
            Challenge        = challenge,
            Completed        = true               // already done
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
            ChallengeId      = challenge.Id,
            Challenge        = challenge,
            Completed        = false,
            CurrentValue     = 3
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        var saved = db.ChallengeProgress.Local.Single();
        Assert.True(saved.Completed);
        Assert.NotNull(saved.CompletedAt);
        Assert.Equal(10, saved.CurrentValue);   // Math.Max(3, 10) = 10 (TargetValue > 0 branch)
    }

    [Fact]
    public async Task CompleteChallengeForStudentAsync_WhenNotCompletedAndTargetZero_KeepsCurrentValue()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: MyId);
        await db.SaveChangesAsync();
        var challenge = SeedChallenge(db, targetValue: 0);  // TargetValue = 0 → if branch not taken
        db.ChallengeProgress.Add(new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId      = challenge.Id,
            Challenge        = challenge,
            Completed        = false,
            CurrentValue     = 5
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeForStudentAsync(
            student.Id, challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        var saved = db.ChallengeProgress.Local.Single();
        Assert.True(saved.Completed);
        Assert.Equal(5, saved.CurrentValue);    // TargetValue = 0 → CurrentValue untouched
    }

    // -----------------------------------------------------------------------
    // DeleteStudentAsync  (SQLite — ExecuteDeleteAsync)
    // -----------------------------------------------------------------------

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
        var student = SeedStudent(db, parentId: MyId, classId: Guid.NewGuid()); // ClassId != null
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
            // SQLite enforces the ParentId → AppUser FK, so the parent user must exist first.
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
}
