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

public class StudentSelfServiceTests
{
    private static readonly Guid MyId    = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser>             _currentUserMock             = new();
    private readonly Mock<IActivityService>         _activityServiceMock         = new();
    private readonly Mock<ILearningActivityService> _learningActivityServiceMock  = new();
    private readonly Mock<IBadgeService>            _badgeServiceMock            = new();
    private readonly Mock<IEventDispatcher>         _eventDispatcherMock         = new();

    public StudentSelfServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(MyId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private StudentSelfService CreateService(AppDbContext db) =>
        new(db,
            _currentUserMock.Object,
            _activityServiceMock.Object,
            _learningActivityServiceMock.Object,
            _badgeServiceMock.Object,
            _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static StudentProfile SeedProfile(AppDbContext db)
    {
        var p = new StudentProfile { UserId = MyId, FirstName = "Alice", LastName = "Smith" };
        db.Students.Add(p);
        return p;
    }

    private static AssignedBook SeedAssignedBook(AppDbContext db, DateTime? endDateUtc = null, int points = 0)
    {
        var book = new Book { Title = "Book", Author = "Author" };
        db.Books.Add(book);
        var ab = new AssignedBook
        {
            ClassId    = OtherId,
            BookId     = book.Id,
            Book       = book,
            EndDateUtc = endDateUtc,
            Points     = points
        };
        db.AssignedBooks.Add(ab);
        return ab;
    }

    private static ReadingProgress SeedReadingProgress(
        AppDbContext db, Guid studentId, AssignedBook ab,
        int currentPage = 0, int? totalPages = 100,
        ProgressStatus status = ProgressStatus.NotStarted,
        DateTime? startedAt = null, DateTime? completedAt = null)
    {
        var rp = new ReadingProgress
        {
            StudentProfileId = studentId,
            AssignedBookId   = ab.Id,
            AssignedBook     = ab,
            CurrentPage      = currentPage,
            TotalPages       = totalPages,
            Status           = status,
            StartedAt        = startedAt,
            CompletedAt      = completedAt
        };
        db.ReadingProgress.Add(rp);
        return rp;
    }

    private static Assignment SeedAssignment(AppDbContext db, int points = 10, DateTime? dueDate = null)
    {
        var a = new Assignment
        {
            ClassId               = OtherId,
            CreatedByTeacherId    = OtherId,
            Title                 = "Assignment",
            Description           = "Desc",
            Points                = points,
            DueDate               = dueDate
        };
        db.Assignments.Add(a);
        return a;
    }

    private static AssignmentProgress SeedAssignmentProgress(
        AppDbContext db, Guid studentId, Assignment assignment,
        ProgressStatus status = ProgressStatus.NotStarted)
    {
        var ap = new AssignmentProgress
        {
            StudentProfileId = studentId,
            AssignmentId     = assignment.Id,
            Assignment       = assignment,
            Status           = status
        };
        db.AssignmentProgress.Add(ap);
        return ap;
    }

    private static Challenge SeedChallenge(AppDbContext db, int targetValue = 10, int points = 20)
    {
        var c = new Challenge
        {
            ClassId     = OtherId,
            Title       = "Challenge",
            Description = "Desc",
            TargetValue = targetValue,
            Points      = points,
            EndDate     = DateTime.UtcNow.AddDays(-1)
        };
        db.Challenges.Add(c);
        return c;
    }

    private static ChallengeProgress SeedChallengeProgress(
        AppDbContext db, Guid studentId, Challenge challenge,
        bool completed = false, DateTime? startedAt = null, int currentValue = 0)
    {
        var cp = new ChallengeProgress
        {
            StudentProfileId = studentId,
            ChallengeId      = challenge.Id,
            Challenge        = challenge,
            Completed        = completed,
            StartedAt        = startedAt,
            CurrentValue     = currentValue
        };
        db.ChallengeProgress.Add(cp);
        return cp;
    }

    // -----------------------------------------------------------------------
    // GetMyDetailsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMyDetailsAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task GetMyDetailsAsync_WhenProfileFoundWithNoActivityLogs_StatsAreZero()
    {
        await using var db = CreateDbContext();
        SeedProfile(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.Data.TotalPagesRead);         // stats = null → ?? 0 null branch
        Assert.Equal(0, result.Data.CompletedAssignments);   // stats = null → ?? 0 null branch
        Assert.Equal(0, result.Data.TotalPoints);            // stats = null → ?? 0 null branch
        Assert.Empty(result.Data.ActivityLast7Days);
    }

    [Fact]
    public async Task GetMyDetailsAsync_WhenStudentHasAllActivityTypes_MapsAllSwitchDescriptions()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var entries = new (ActivityType type, int? pages, int? pts)[]
        {
            (ActivityType.ReadingProgress,             10, 5),  // PagesRead non-null; PointsEarned non-null
            (ActivityType.ReadingProgress,           null, null), // PagesRead null → ?? 0; PointsEarned null → ?? 0
            (ActivityType.AssignmentCompleted,       null, 10),
            (ActivityType.AssignmentStarted,         null, null),
            (ActivityType.ChallengeCompleted,        null, null),
            (ActivityType.ChallengeProgressUpdated,  null, null),
            (ActivityType.LearningActivityCompleted, null, null),
            (ActivityType.LearningActivityStarted,   null, null),
            ((ActivityType)99,                       null, null), // default switch arm
        };

        foreach (var (type, pages, pts) in entries)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                StudentProfileId = profile.Id,
                ActivityType     = type,
                ReferenceType    = ActivityReferenceType.AssignedBook,
                ReferenceId      = Guid.NewGuid(),
                Date             = today,
                PagesRead        = pages,
                PointsEarned     = pts
            });
        }
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(0, result.Data.TotalPoints);  // stats non-null → ?? 0 non-null branch

        var descs = result.Data.ActivityLast7Days.Select(a => a.Description).ToList();
        Assert.Contains("Прочел 10 стр.",                descs);  // PagesRead=10
        Assert.Contains("Прочел 0 стр.",                 descs);  // PagesRead=null → ?? 0
        Assert.Contains("Завърши задача",                descs);
        Assert.Contains("Стартира задача",               descs);
        Assert.Contains("Завърши предизвикателство",     descs);
        Assert.Contains("Актуализира предизвикателство", descs);
        Assert.Contains("Завърши учебна дейност",        descs);
        Assert.Contains("Стартира учебна дейност",       descs);
        Assert.Contains("Активност",                     descs);  // default arm

        // PointsEarned null → ?? 0 non-zero total still present from other entries
        Assert.Contains(result.Data.ActivityLast7Days, a => a.PointsEarned == 0); // null → 0
        Assert.Contains(result.Data.ActivityLast7Days, a => a.PointsEarned > 0);  // non-null → value
    }

    [Fact]
    public async Task GetMyDetailsAsync_WhenReadingProgressExists_MapsIsExpiredCorrectly()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();

        var expiredAb = SeedAssignedBook(db, endDateUtc: DateTime.UtcNow.AddDays(-1));  // IsExpired=true
        var openAb    = SeedAssignedBook(db, endDateUtc: null);                         // IsExpired=false
        await db.SaveChangesAsync();

        SeedReadingProgress(db, profile.Id, expiredAb);
        SeedReadingProgress(db, profile.Id, openAb);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Reading, r => r.IsExpired);    // HasValue=true, past → true
        Assert.Contains(result.Data.Reading, r => !r.IsExpired);   // HasValue=false → false
    }

    [Fact]
    public async Task GetMyDetailsAsync_WhenAssignmentProgressExists_MapsIsExpiredCorrectly()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();

        var pastAssignment = SeedAssignment(db, dueDate: DateTime.UtcNow.AddDays(-1));  // IsExpired=true
        var openAssignment = SeedAssignment(db, dueDate: null);                          // IsExpired=false
        await db.SaveChangesAsync();

        SeedAssignmentProgress(db, profile.Id, pastAssignment);
        SeedAssignmentProgress(db, profile.Id, openAssignment);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Assignments, a => a.IsExpired);    // DueDate past → true
        Assert.Contains(result.Data.Assignments, a => !a.IsExpired);   // DueDate null → false
    }

    [Fact]
    public async Task GetMyDetailsAsync_WhenLearningActivitiesExist_MapsIsExpiredCorrectly()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();

        var expiredLa = new LearningActivity
        {
            ClassId = OtherId, CreatedByTeacherId = OtherId,
            Title = "Expired", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            DueDateUtc = DateTime.UtcNow.AddDays(-1)   // IsExpired=true
        };
        var openLa = new LearningActivity
        {
            ClassId = OtherId, CreatedByTeacherId = OtherId,
            Title = "Open", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            DueDateUtc = null                           // IsExpired=false
        };
        db.LearningActivities.Add(expiredLa);
        db.LearningActivities.Add(openLa);
        await db.SaveChangesAsync();

        db.StudentLearningActivityProgress.Add(new StudentLearningActivityProgress
        {
            StudentProfileId = profile.Id, LearningActivityId = expiredLa.Id, LearningActivity = expiredLa
        });
        db.StudentLearningActivityProgress.Add(new StudentLearningActivityProgress
        {
            StudentProfileId = profile.Id, LearningActivityId = openLa.Id, LearningActivity = openLa
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.LearningActivities, la => la.IsExpired);
        Assert.Contains(result.Data.LearningActivities, la => !la.IsExpired);
    }

    [Fact]
    public async Task GetMyDetailsAsync_WhenChallengeProgressExists_MapsStartedAndIsExpired()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();

        var challenge = SeedChallenge(db);  // EndDate = past → IsExpired=true
        await db.SaveChangesAsync();

        SeedChallengeProgress(db, profile.Id, challenge, startedAt: DateTime.UtcNow);  // Started=true
        SeedChallengeProgress(db, profile.Id, challenge, startedAt: null);             // Started=false
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyDetailsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains(result.Data.Challenges, c => c.Started);
        Assert.Contains(result.Data.Challenges, c => !c.Started);
        Assert.All(result.Data.Challenges, c => Assert.True(c.IsExpired));  // all past → IsExpired
    }

    // -----------------------------------------------------------------------
    // UpdateReadingProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenCurrentPageNegative_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(Guid.NewGuid(), -1, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("cannot be negative", result.Error);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(Guid.NewGuid(), 5, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedProfile(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(Guid.NewGuid(), 5, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Progress not found.", result.Error);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenDeadlinePassed_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, endDateUtc: DateTime.UtcNow.AddDays(-1));
        SeedReadingProgress(db, profile.Id, ab);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(ab.Id, 10, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Deadline has passed", result.Error);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenCurrentPageLessThanPrevious_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, endDateUtc: null);
        SeedReadingProgress(db, profile.Id, ab, currentPage: 50);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(ab.Id, 30, CancellationToken.None);  // 30 < 50

        Assert.False(result.Success);
        Assert.Contains("cannot be less than previous", result.Error);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenTotalPagesNull_SetsInProgress()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, endDateUtc: null);
        // StartedAt pre-set → false branch of (StartedAt == null)
        SeedReadingProgress(db, profile.Id, ab, totalPages: null,
            startedAt: DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(ab.Id, 50, CancellationToken.None);

        Assert.True(result.Success);
        var rp = db.ReadingProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, rp.Status);
        Assert.Equal(50, rp.CurrentPage);
        // StartedAt not overwritten (false branch of StartedAt == null check)
        Assert.True(rp.StartedAt < DateTime.UtcNow.AddMinutes(-1));

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookCompletedEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenBookCompletedFirstTime_SetsCompletedAndPublishesEvent()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, endDateUtc: null);
        // StartedAt = null → true branch of (StartedAt == null), will be set
        SeedReadingProgress(db, profile.Id, ab, currentPage: 0, totalPages: 100,
            status: ProgressStatus.NotStarted, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(ab.Id, 100, CancellationToken.None);

        Assert.True(result.Success);
        var rp = db.ReadingProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, rp.Status);
        Assert.NotNull(rp.StartedAt);   // null → set (true branch)
        Assert.NotNull(rp.CompletedAt); // !wasAlreadyCompleted → set

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookCompletedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task UpdateReadingProgressAsync_WhenAlreadyCompleted_DoesNotOverwriteCompletedAtOrPublishEvent()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();
        var ab          = SeedAssignedBook(db, endDateUtc: null);
        var completedAt = DateTime.UtcNow.AddDays(-2);
        var rp = SeedReadingProgress(db, profile.Id, ab, currentPage: 100, totalPages: 100,
            status: ProgressStatus.Completed, startedAt: DateTime.UtcNow.AddDays(-5));
        rp.CompletedAt = completedAt;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateReadingProgressAsync(ab.Id, 100, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(completedAt, db.ReadingProgress.Local.Single().CompletedAt); // !wasAlreadyCompleted=false → unchanged

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookCompletedEvent>(), CancellationToken.None),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // StartAssignmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task StartAssignmentAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.StartAssignmentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task StartAssignmentAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedProfile(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartAssignmentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Assignment not found.", result.Error);
    }

    [Fact]
    public async Task StartAssignmentAsync_WhenStatusNotStarted_SetsInProgress()
    {
        await using var db = CreateDbContext();
        var profile    = SeedProfile(db);
        var assignment = SeedAssignment(db);
        await db.SaveChangesAsync();
        SeedAssignmentProgress(db, profile.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartAssignmentAsync(assignment.Id, CancellationToken.None);

        Assert.True(result.Success);
        var ap = db.AssignmentProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, ap.Status);
        Assert.NotNull(ap.StartedAt);
    }

    [Fact]
    public async Task StartAssignmentAsync_WhenStatusAlreadyInProgress_ReturnsOkWithoutSaving()
    {
        await using var db = CreateDbContext();
        var profile    = SeedProfile(db);
        var assignment = SeedAssignment(db);
        await db.SaveChangesAsync();
        // Status != NotStarted → early Ok (true branch of Status != NotStarted)
        SeedAssignmentProgress(db, profile.Id, assignment, ProgressStatus.InProgress);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartAssignmentAsync(assignment.Id, CancellationToken.None);

        Assert.True(result.Success);
        // Status unchanged (early return)
        Assert.Equal(ProgressStatus.InProgress, db.AssignmentProgress.Local.Single().Status);
    }

    // -----------------------------------------------------------------------
    // CompleteAssignmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CompleteAssignmentAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CompleteAssignmentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedProfile(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAssignmentAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Assignment not found.", result.Error);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_WhenAlreadyCompleted_ReturnsOkWithoutSaving()
    {
        await using var db = CreateDbContext();
        var profile    = SeedProfile(db);
        var assignment = SeedAssignment(db);
        await db.SaveChangesAsync();
        // Status == Completed → early Ok (true branch)
        SeedAssignmentProgress(db, profile.Id, assignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAssignmentAsync(assignment.Id, CancellationToken.None);

        Assert.True(result.Success);
        _activityServiceMock.Verify(
            a => a.LogAssignmentCompletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteAssignmentAsync_WhenNotCompleted_SetsCompletedAndPublishesEvent()
    {
        await using var db = CreateDbContext();
        var profile    = SeedProfile(db);
        var assignment = SeedAssignment(db, points: 50);
        await db.SaveChangesAsync();
        SeedAssignmentProgress(db, profile.Id, assignment, ProgressStatus.InProgress);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAssignmentAsync(assignment.Id, CancellationToken.None);

        Assert.True(result.Success);
        var ap = db.AssignmentProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, ap.Status);
        Assert.NotNull(ap.CompletedAt);

        _activityServiceMock.Verify(
            a => a.LogAssignmentCompletedAsync(profile.Id, assignment.Id, 50, CancellationToken.None),
            Times.Once);
        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<AssignmentCompletedEvent>(), CancellationToken.None),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // StartChallengeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task StartChallengeAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.StartChallengeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task StartChallengeAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedProfile(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Challenge not found.", result.Error);
    }

    [Fact]
    public async Task StartChallengeAsync_WhenAlreadyStarted_ReturnsOkWithoutSaving()
    {
        await using var db = CreateDbContext();
        var profile   = SeedProfile(db);
        var challenge = SeedChallenge(db);
        await db.SaveChangesAsync();
        // StartedAt is not null → early Ok (true branch)
        SeedChallengeProgress(db, profile.Id, challenge, startedAt: DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeAsync(challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(db.ChallengeProgress.Local.Single().StartedAt);
    }

    [Fact]
    public async Task StartChallengeAsync_WhenNotYetStarted_SetsStartedAt()
    {
        await using var db = CreateDbContext();
        var profile   = SeedProfile(db);
        var challenge = SeedChallenge(db);
        await db.SaveChangesAsync();
        SeedChallengeProgress(db, profile.Id, challenge, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.StartChallengeAsync(challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(db.ChallengeProgress.Local.Single().StartedAt);
    }

    // -----------------------------------------------------------------------
    // CompleteChallengeAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CompleteChallengeAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CompleteChallengeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task CompleteChallengeAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedProfile(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Challenge not found.", result.Error);
    }

    [Fact]
    public async Task CompleteChallengeAsync_WhenAlreadyCompleted_ReturnsOkWithoutSaving()
    {
        await using var db = CreateDbContext();
        var profile   = SeedProfile(db);
        var challenge = SeedChallenge(db);
        await db.SaveChangesAsync();
        // Completed=true → early Ok (true branch)
        SeedChallengeProgress(db, profile.Id, challenge, completed: true);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeAsync(challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        _activityServiceMock.Verify(
            a => a.LogChallengeCompletedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompleteChallengeAsync_WhenNotCompletedAndTargetPositive_UpdatesCurrentValueAndPublishesEvent()
    {
        await using var db = CreateDbContext();
        var profile   = SeedProfile(db);
        var challenge = SeedChallenge(db, targetValue: 10, points: 30);
        await db.SaveChangesAsync();
        // currentValue=3, targetValue=10 → Math.Max(3,10)=10 (TargetValue > 0 branch)
        SeedChallengeProgress(db, profile.Id, challenge, completed: false, currentValue: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeAsync(challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        var cp = db.ChallengeProgress.Local.Single();
        Assert.True(cp.Completed);
        Assert.NotNull(cp.CompletedAt);
        Assert.Equal(10, cp.CurrentValue);  // Math.Max(3, 10) = 10

        _activityServiceMock.Verify(
            a => a.LogChallengeCompletedAsync(profile.Id, challenge.Id, 30, CancellationToken.None),
            Times.Once);
        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<ChallengeCompletedEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CompleteChallengeAsync_WhenNotCompletedAndTargetZero_KeepsCurrentValue()
    {
        await using var db = CreateDbContext();
        var profile   = SeedProfile(db);
        var challenge = SeedChallenge(db, targetValue: 0);  // TargetValue=0 → if branch not taken
        await db.SaveChangesAsync();
        SeedChallengeProgress(db, profile.Id, challenge, completed: false, currentValue: 5);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteChallengeAsync(challenge.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(5, db.ChallengeProgress.Local.Single().CurrentValue);  // unchanged
    }

    // -----------------------------------------------------------------------
    // GetMyBadgesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMyBadgesAsync_WhenProfileNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetMyBadgesAsync(CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student profile not found.", result.Error);
    }

    [Fact]
    public async Task GetMyBadgesAsync_WhenProfileFound_ReturnsBadgesOrderedByAwardedAt()
    {
        await using var db = CreateDbContext();
        var profile = SeedProfile(db);
        await db.SaveChangesAsync();

        var badge1 = new Badge { Name = "B1", Code = "CODE1", Description = "D1", Icon = "I1" };
        var badge2 = new Badge { Name = "B2", Code = "CODE2", Description = "D2", Icon = "I2" };
        db.Badges.Add(badge1);
        db.Badges.Add(badge2);
        await db.SaveChangesAsync();

        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow;
        db.StudentBadges.Add(new StudentBadge { StudentProfileId = profile.Id, BadgeId = badge1.Id, Badge = badge1, AwardedAt = older });
        db.StudentBadges.Add(new StudentBadge { StudentProfileId = profile.Id, BadgeId = badge2.Id, Badge = badge2, AwardedAt = newer });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyBadgesAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("CODE2", result.Data[0].Code);  // newest first (OrderByDescending)
        Assert.Equal("CODE1", result.Data[1].Code);
    }
}
