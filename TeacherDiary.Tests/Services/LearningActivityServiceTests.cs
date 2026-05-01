using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class LearningActivityServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public LearningActivityServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private LearningActivityService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object);

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
        AppDbContext db, Guid classId, bool isActive = true)
    {
        var s = new StudentProfile
        {
            ClassId = classId,
            FirstName = "Alice",
            LastName = "Smith",
            IsActive = isActive
        };
        db.Students.Add(s);
        return s;
    }

    private static (Book book, AssignedBook ab) SeedBookAndAssignedBook(
        AppDbContext db, Guid classId, int? totalPages = 200)
    {
        var book = new Book { Title = "Test Book", Author = "Author", TotalPages = totalPages };
        db.Books.Add(book);
        var ab = new AssignedBook
        {
            ClassId = classId,
            BookId = book.Id,
            Book = book,
            StartDateUtc = DateTime.UtcNow,
            EndDateUtc = DateTime.UtcNow.AddDays(30)
        };
        db.AssignedBooks.Add(ab);
        return (book, ab);
    }

    private static LearningActivity SeedLearningActivityForBook(
        AppDbContext db, Guid classId, Guid assignedBookId, int? targetValue = 200)
    {
        var la = new LearningActivity
        {
            ClassId = classId,
            CreatedByTeacherId = TeacherId,
            Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            Title = "Reading Activity",
            AssignedBookId = assignedBookId,
            TargetValue = targetValue
        };
        db.LearningActivities.Add(la);
        return la;
    }

    private static LearningActivity SeedLearningActivityForAssignment(
        AppDbContext db, Guid classId, Guid assignmentId)
    {
        var la = new LearningActivity
        {
            ClassId = classId,
            CreatedByTeacherId = TeacherId,
            Type = LearningActivityType.Assignment,
            Status = LearningActivityStatus.Active,
            Title = "Assignment Activity",
            AssignmentId = assignmentId
        };
        db.LearningActivities.Add(la);
        return la;
    }

    private static LearningActivity SeedLearningActivityForChallenge(
        AppDbContext db, Guid classId, Guid challengeId)
    {
        var la = new LearningActivity
        {
            ClassId = classId,
            CreatedByTeacherId = TeacherId,
            Type = LearningActivityType.Challenge,
            Status = LearningActivityStatus.Active,
            Title = "Challenge Activity",
            ChallengeId = challengeId
        };
        db.LearningActivities.Add(la);
        return la;
    }

    private static StudentLearningActivityProgress SeedProgress(
        AppDbContext db, Guid studentId, LearningActivity la,
        int? targetValue = 200,
        DateTime? startedAt = null,
        DateTime? completedAt = null)
    {
        var p = new StudentLearningActivityProgress
        {
            StudentProfileId = studentId,
            LearningActivityId = la.Id,
            LearningActivity = la,
            Status = ProgressStatus.NotStarted,
            CurrentValue = 0,
            TargetValue = targetValue,
            StartedAt = startedAt,
            CompletedAt = completedAt
        };
        db.StudentLearningActivityProgress.Add(p);
        return p;
    }

    // -----------------------------------------------------------------------
    // CreateForAssignedBookAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateForAssignedBookAsync_WhenNoActiveStudents_CreatesActivityWithNoProgressRows()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var (_, ab) = SeedBookAndAssignedBook(db, cls.Id);
        SeedStudent(db, cls.Id, isActive: false);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var activityId = await service.CreateForAssignedBookAsync(ab, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, activityId);
        Assert.Equal(1, db.LearningActivities.Count());
        Assert.Equal(0, db.StudentLearningActivityProgress.Count());

        var la = db.LearningActivities.Single();
        Assert.Equal(LearningActivityType.Reading, la.Type);
        Assert.Equal("Test Book", la.Title);
        Assert.Equal(ab.Id, la.AssignedBookId);
        Assert.Equal(200, la.TargetValue);
    }

    [Fact]
    public async Task CreateForAssignedBookAsync_WhenActiveStudentsExist_CreatesProgressPerStudent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var (_, ab) = SeedBookAndAssignedBook(db, cls.Id);
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id, isActive: false);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.CreateForAssignedBookAsync(ab, CancellationToken.None);

        Assert.Equal(2, db.StudentLearningActivityProgress.Count());
        Assert.All(db.StudentLearningActivityProgress,
            p => Assert.Equal(ProgressStatus.NotStarted, p.Status));
    }

    // -----------------------------------------------------------------------
    // CreateForAssignmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateForAssignmentAsync_WhenActiveStudentsExist_CreatesActivityAndProgressPerStudent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var assignment = new Assignment
        {
            ClassId = cls.Id,
            CreatedByTeacherId = TeacherId,
            Title = "Test Assignment",
            Description = "Do it",
            Subject = "Math",
            DueDate = DateTime.UtcNow.AddDays(7),
            Points = 10
        };

        var activityId = await service.CreateForAssignmentAsync(assignment, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, activityId);
        var la = db.LearningActivities.Single();
        Assert.Equal(LearningActivityType.Assignment, la.Type);
        Assert.Equal("Test Assignment", la.Title);
        Assert.Equal(assignment.Id, la.AssignmentId);
        Assert.Equal(2, db.StudentLearningActivityProgress.Count());
    }

    // -----------------------------------------------------------------------
    // CreateForChallengeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateForChallengeAsync_WhenActiveStudentsExist_CreatesActivityAndProgressPerStudent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var challenge = new Challenge
        {
            ClassId = cls.Id,
            Class = cls,
            Title = "Read 5 Books",
            Description = "Challenge",
            TargetValue = 5,
            Points = 50,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var activityId = await service.CreateForChallengeAsync(challenge, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, activityId);
        var la = db.LearningActivities.Single();
        Assert.Equal(LearningActivityType.Challenge, la.Type);
        Assert.Equal("Read 5 Books", la.Title);
        Assert.Equal(challenge.Id, la.ChallengeId);
        Assert.Equal(5, la.TargetValue);
        Assert.Equal(1, db.StudentLearningActivityProgress.Count());
        Assert.Equal(5, db.StudentLearningActivityProgress.Single().TargetValue);
    }

    // -----------------------------------------------------------------------
    // UpdateReadingProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenProgressNotFound_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        // No progress seeded — should silently return
        await service.UpdateReadingProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), 50, CancellationToken.None);

        Assert.Equal(0, db.StudentLearningActivityProgress.Count());
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenBelowTarget_SetsInProgressAndStartedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var (_, ab) = SeedBookAndAssignedBook(db, cls.Id);
        var student = SeedStudent(db, cls.Id);
        var la = SeedLearningActivityForBook(db, cls.Id, ab.Id, targetValue: 200);
        var progress = SeedProgress(db, student.Id, la, targetValue: 200, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateReadingProgressAsync(student.Id, ab.Id, 50, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, p.Status);
        Assert.Equal(50, p.CurrentValue);
        Assert.NotNull(p.StartedAt);   // was null → now set
        Assert.Null(p.CompletedAt);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenReachesTarget_SetsCompletedAndKeepsStartedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var (_, ab) = SeedBookAndAssignedBook(db, cls.Id);
        var student = SeedStudent(db, cls.Id);
        var la = SeedLearningActivityForBook(db, cls.Id, ab.Id, targetValue: 200);
        var existingStartedAt = DateTime.UtcNow.AddDays(-3);
        SeedProgress(db, student.Id, la, targetValue: 200, startedAt: existingStartedAt);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateReadingProgressAsync(student.Id, ab.Id, 200, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, p.Status);
        Assert.Equal(200, p.CurrentValue);
        Assert.Equal(existingStartedAt, p.StartedAt);  // pre-set value kept (??= not-null branch)
        Assert.NotNull(p.CompletedAt);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenTargetValueIsNull_SetsInProgressWithoutCompleting()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var (_, ab) = SeedBookAndAssignedBook(db, cls.Id);
        var student = SeedStudent(db, cls.Id);
        // targetValue: null → TargetValue.HasValue = false → if on line 145 not entered
        var la = SeedLearningActivityForBook(db, cls.Id, ab.Id, targetValue: null);
        SeedProgress(db, student.Id, la, targetValue: null, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateReadingProgressAsync(student.Id, ab.Id, 50, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, p.Status);
        Assert.Null(p.CompletedAt);
    }

    // -----------------------------------------------------------------------
    // UpdateAssignmentProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAssignmentProgressAsync_WhenProgressNotFound_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.UpdateAssignmentProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), completed: true, score: null, CancellationToken.None);

        Assert.Equal(0, db.StudentLearningActivityProgress.Count());
    }

    [Fact]
    public async Task UpdateAssignmentProgressAsync_WhenNotCompleted_SetsInProgressAndStartedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignmentId = Guid.NewGuid();
        var la = SeedLearningActivityForAssignment(db, cls.Id, assignmentId);
        SeedProgress(db, student.Id, la, targetValue: null, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateAssignmentProgressAsync(
            student.Id, assignmentId, completed: false, score: 5, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, p.Status);
        Assert.Equal(5, p.Score);
        Assert.NotNull(p.StartedAt);   // null → set
        Assert.Null(p.CompletedAt);
    }

    [Fact]
    public async Task UpdateAssignmentProgressAsync_WhenCompletedFirstTime_SetsCompletedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignmentId = Guid.NewGuid();
        var la = SeedLearningActivityForAssignment(db, cls.Id, assignmentId);
        SeedProgress(db, student.Id, la, targetValue: null, startedAt: null, completedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateAssignmentProgressAsync(
            student.Id, assignmentId, completed: true, score: 10, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, p.Status);
        Assert.Equal(10, p.Score);
        Assert.NotNull(p.StartedAt);
        Assert.NotNull(p.CompletedAt);
    }

    [Fact]
    public async Task UpdateAssignmentProgressAsync_WhenAlreadyCompleted_DoesNotOverwriteCompletedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignmentId = Guid.NewGuid();
        var la = SeedLearningActivityForAssignment(db, cls.Id, assignmentId);
        var originalStartedAt = DateTime.UtcNow.AddDays(-5);
        var originalCompletedAt = DateTime.UtcNow.AddDays(-1);
        SeedProgress(db, student.Id, la,
            targetValue: null,
            startedAt: originalStartedAt,
            completedAt: originalCompletedAt);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateAssignmentProgressAsync(
            student.Id, assignmentId, completed: true, score: null, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, p.Status);
        Assert.Equal(originalStartedAt, p.StartedAt);      // ??= kept pre-set value
        Assert.Equal(originalCompletedAt, p.CompletedAt);  // not overwritten
    }

    // -----------------------------------------------------------------------
    // UpdateChallengeProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateChallengeProgressAsync_WhenProgressNotFound_DoesNothing()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.UpdateChallengeProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), currentValue: 3, completed: false, CancellationToken.None);

        Assert.Equal(0, db.StudentLearningActivityProgress.Count());
    }

    [Fact]
    public async Task UpdateChallengeProgressAsync_WhenCompleted_SetsCompletedStatus()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var challengeId = Guid.NewGuid();
        var la = SeedLearningActivityForChallenge(db, cls.Id, challengeId);
        SeedProgress(db, student.Id, la, targetValue: 5);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateChallengeProgressAsync(
            student.Id, challengeId, currentValue: 5, completed: true, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, p.Status);
        Assert.Equal(5, p.CurrentValue);
        Assert.NotNull(p.CompletedAt);
    }

    [Fact]
    public async Task UpdateChallengeProgressAsync_WhenNotCompleted_SetsInProgressStatus()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var challengeId = Guid.NewGuid();
        var la = SeedLearningActivityForChallenge(db, cls.Id, challengeId);
        SeedProgress(db, student.Id, la, targetValue: 5);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.UpdateChallengeProgressAsync(
            student.Id, challengeId, currentValue: 3, completed: false, CancellationToken.None);

        var p = db.StudentLearningActivityProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, p.Status);
        Assert.Equal(3, p.CurrentValue);
        Assert.Null(p.CompletedAt);
    }
}
