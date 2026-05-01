using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Reading;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class ReadingServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId     = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser>            _currentUserMock            = new();
    private readonly Mock<IActivityService>        _activityServiceMock        = new();
    private readonly Mock<ILearningActivityService> _learningActivityServiceMock = new();
    private readonly Mock<IBadgeService>           _badgeServiceMock           = new();
    private readonly Mock<IEventDispatcher>        _eventDispatcherMock        = new();

    public ReadingServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // SQLite in-memory required for ExecuteDeleteAsync.
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

    private ReadingService CreateService(AppDbContext db) =>
        new(db,
            _currentUserMock.Object,
            _activityServiceMock.Object,
            _learningActivityServiceMock.Object,
            _badgeServiceMock.Object,
            _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static Book SeedBook(AppDbContext db, string title = "Test Book", int? totalPages = 100)
    {
        var b = new Book { Title = title, Author = "Author", TotalPages = totalPages };
        db.Books.Add(b);
        return b;
    }

    private static Class SeedClass(AppDbContext db, Guid? teacherId = null, Guid? orgId = null)
    {
        var cls = new Class
        {
            OrganizationId = orgId ?? OrgId,
            TeacherId      = teacherId ?? TeacherId,
            Name           = "3A",
            Grade          = 3,
            SchoolYear     = "2024/2025"
        };
        db.Classes.Add(cls);
        return cls;
    }

    private static AssignedBook SeedAssignedBook(
        AppDbContext db, Guid classId, Guid bookId, Class? cls = null,
        DateTime? endDateUtc = null, int points = 0)
    {
        var ab = new AssignedBook
        {
            ClassId    = classId,
            BookId     = bookId,
            EndDateUtc = endDateUtc,
            Points     = points
        };
        if (cls is not null) ab.Class = cls;
        db.AssignedBooks.Add(ab);
        return ab;
    }

    private static StudentProfile SeedStudent(AppDbContext db, Guid? classId = null, Guid? parentId = null)
    {
        var s = new StudentProfile { ClassId = classId, ParentId = parentId, FirstName = "Alice", LastName = "Smith" };
        db.Students.Add(s);
        return s;
    }

    private static ReadingProgress SeedReadingProgress(
        AppDbContext db, Guid studentId, Guid assignedBookId, AssignedBook? ab = null,
        int currentPage = 0, int? totalPages = 100,
        ProgressStatus status = ProgressStatus.NotStarted,
        DateTime? startedAt = null)
    {
        var rp = new ReadingProgress
        {
            StudentProfileId = studentId,
            AssignedBookId   = assignedBookId,
            CurrentPage      = currentPage,
            TotalPages       = totalPages,
            Status           = status,
            StartedAt        = startedAt
        };
        if (ab is not null) rp.AssignedBook = ab;
        db.ReadingProgress.Add(rp);
        return rp;
    }

    // -----------------------------------------------------------------------
    // CreateBookAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateBookAsync_WhenUserIdIsEmpty_ReturnsFail()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.Empty);
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateBookAsync(
            new BookCreateRequest { Title = "X", Author = "Y" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Unauthorized.", result.Error);
    }

    [Fact]
    public async Task CreateBookAsync_WhenBookAlreadyExists_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedBook(db, title: "Dup");
        db.Books.Local.Single().Author = "Same";
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CreateBookAsync(
            new BookCreateRequest { Title = "Dup", Author = "Same" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task CreateBookAsync_WhenValid_CreatesBookAndReturnsId()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateBookAsync(
            new BookCreateRequest { Title = "New Book", Author = "Author", TotalPages = 200 },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);
        Assert.Equal(1, db.Books.Count());
    }

    // -----------------------------------------------------------------------
    // AssignBookToClassAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AssignBookToClassAsync_WhenStartDateAfterEndDate_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.AssignBookToClassAsync(
            Guid.NewGuid(),
            new AssignBookRequest
            {
                BookId       = Guid.NewGuid(),
                StartDateUtc = DateTime.UtcNow.AddDays(5),
                EndDateUtc   = DateTime.UtcNow
            },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid date range.", result.Error);
    }

    [Fact]
    public async Task AssignBookToClassAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.AssignBookToClassAsync(
            Guid.NewGuid(),
            new AssignBookRequest { BookId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task AssignBookToClassAsync_WhenBookNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.AssignBookToClassAsync(
            cls.Id,
            new AssignBookRequest { BookId = Guid.NewGuid() },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Book not found.", result.Error);
    }

    [Fact]
    public async Task AssignBookToClassAsync_WhenValid_CreatesAssignedBookAndProgressRowsPerStudent()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();
        SeedStudent(db, classId: cls.Id);
        SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.AssignBookToClassAsync(
            cls.Id,
            new AssignBookRequest { BookId = book.Id, Points = 50 },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, db.AssignedBooks.Count());
        Assert.Equal(2, db.ReadingProgress.Count());  // one row per student

        _learningActivityServiceMock.Verify(
            s => s.CreateForAssignedBookAsync(It.IsAny<AssignedBook>(), CancellationToken.None),
            Times.Once);
        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookAssignedEvent>(), CancellationToken.None),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // UpdateProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateProgressAsync_WhenCurrentPageNegative_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), -1, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("cannot be negative", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), 5, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenParentMismatch_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: Guid.NewGuid()); // different parent
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, Guid.NewGuid(), 5, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Forbidden.", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenParentIdIsNull_ReturnsForbidden()
    {
        await using var db = CreateDbContext();
        // parentId: null → Nullable<Guid> null branch of != comparison → null != UserId → true → Forbidden
        var student = SeedStudent(db, parentId: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, Guid.NewGuid(), 5, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Forbidden.", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: TeacherId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, Guid.NewGuid(), 5, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Progress not found.", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenDeadlinePassed_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: TeacherId);
        var book    = SeedBook(db);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, Guid.NewGuid(), book.Id, endDateUtc: DateTime.UtcNow.AddDays(-1));
        SeedReadingProgress(db, student.Id, ab.Id, ab: ab);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, ab.Id, 10, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Deadline has passed", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenCurrentPageLessThanPrevious_ReturnsFail_AndSetsStartedAt()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: TeacherId);
        var book    = SeedBook(db);
        await db.SaveChangesAsync();
        // EndDateUtc = null → not locked; StartedAt = null → will be set; currentPage=5 then try page 3
        var ab = SeedAssignedBook(db, Guid.NewGuid(), book.Id, endDateUtc: null);
        SeedReadingProgress(db, student.Id, ab.Id, ab: ab, currentPage: 5, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, ab.Id, 3, CancellationToken.None);  // 3 < 5 → Fail

        Assert.False(result.Success);
        Assert.Contains("cannot be less than previous", result.Error);
        // StartedAt was null → set before the backwards check fires
        Assert.NotNull(db.ReadingProgress.Local.Single().StartedAt);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenTotalPagesNull_SetsInProgress()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: TeacherId);
        var book    = SeedBook(db, totalPages: null);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, Guid.NewGuid(), book.Id, endDateUtc: null);
        // TotalPages = null → no completion check; StartedAt already set → not overwritten
        SeedReadingProgress(db, student.Id, ab.Id, ab: ab,
            totalPages: null, startedAt: DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, ab.Id, 50, CancellationToken.None);

        Assert.True(result.Success);
        var rp = db.ReadingProgress.Local.Single();
        Assert.Equal(ProgressStatus.InProgress, rp.Status);
        Assert.Equal(50, rp.CurrentPage);
        // StartedAt pre-set → not overwritten (not-null branch of StartedAt == null check)
        Assert.True(rp.StartedAt < DateTime.UtcNow);

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookCompletedEvent>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenBookCompletedFirstTime_SetsCompletedAndPublishesEvent()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: TeacherId);
        var book    = SeedBook(db, totalPages: 100);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, Guid.NewGuid(), book.Id, endDateUtc: null);
        // currentPage=0, totalPages=100, status=NotStarted → first completion when page reaches 100
        SeedReadingProgress(db, student.Id, ab.Id, ab: ab,
            currentPage: 0, totalPages: 100, status: ProgressStatus.NotStarted, startedAt: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, ab.Id, 100, CancellationToken.None);

        Assert.True(result.Success);
        var rp = db.ReadingProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, rp.Status);
        Assert.NotNull(rp.CompletedAt);    // !wasAlreadyCompleted → sets CompletedAt
        Assert.NotNull(rp.StartedAt);      // StartedAt was null → set before check

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookCompletedEvent>(), CancellationToken.None),
            Times.Once);  // bookCompleted = true → event published
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenAlreadyCompleted_DoesNotOverwriteCompletedAtOrPublishEvent()
    {
        await using var db = CreateDbContext();
        var student = SeedStudent(db, parentId: TeacherId);
        var book    = SeedBook(db, totalPages: 100);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, Guid.NewGuid(), book.Id, endDateUtc: null);
        var completedAt = DateTime.UtcNow.AddDays(-2);
        // wasAlreadyCompleted = true when starting the update
        SeedReadingProgress(db, student.Id, ab.Id, ab: ab,
            currentPage: 100, totalPages: 100, status: ProgressStatus.Completed,
            startedAt: DateTime.UtcNow.AddDays(-5));
        db.ReadingProgress.Local.Single().CompletedAt = completedAt;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            student.Id, ab.Id, 100, CancellationToken.None);

        Assert.True(result.Success);
        var rp = db.ReadingProgress.Local.Single();
        Assert.Equal(ProgressStatus.Completed, rp.Status);
        Assert.Equal(completedAt, rp.CompletedAt);  // !wasAlreadyCompleted=false → CompletedAt unchanged

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<BookCompletedEvent>(), CancellationToken.None),
            Times.Never);  // bookCompleted = false → no event
    }

    // -----------------------------------------------------------------------
    // GetBooksAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetBooksAsync_WhenGradeLevelNull_ReturnsAllBooks()
    {
        await using var db = CreateDbContext();
        SeedBook(db, title: "A");
        SeedBook(db, title: "B");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetBooksAsync(null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);  // no gradeLevel filter applied
    }

    [Fact]
    public async Task GetBooksAsync_WhenGradeLevelSet_ReturnsOnlyMatchingBooks()
    {
        await using var db = CreateDbContext();
        var b1 = SeedBook(db, title: "Grade3");
        var b2 = SeedBook(db, title: "Grade5");
        b1.GradeLevel = 3;
        b2.GradeLevel = 5;
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetBooksAsync(3, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal("Grade3", result.Data[0].Title);
    }

    // -----------------------------------------------------------------------
    // GetAssignedBooksAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAssignedBooksAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetAssignedBooksAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetAssignedBooksAsync_WhenClassFound_ReturnsBooksWithIsExpired()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();

        // expired → IsExpired = true (EndDateUtc.HasValue=true, past)
        SeedAssignedBook(db, cls.Id, book.Id, endDateUtc: DateTime.UtcNow.AddDays(-1));
        // no end date → IsExpired = false (HasValue=false)
        SeedAssignedBook(db, cls.Id, book.Id, endDateUtc: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetAssignedBooksAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, b => b.IsExpired);    // EndDateUtc past → true
        Assert.Contains(result.Data, b => !b.IsExpired);   // EndDateUtc null → false
    }

    // -----------------------------------------------------------------------
    // GetStudentProgressForBookAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStudentProgressForBookAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentProgressForBookAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetStudentProgressForBookAsync_WhenClassFound_ReturnsStudentProgress()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();
        var ab      = SeedAssignedBook(db, cls.Id, book.Id);
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();
        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = ab.Id,
            StudentProfile   = student,
            CurrentPage      = 10
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentProgressForBookAsync(
            cls.Id, ab.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal(10, result.Data[0].CurrentPage);
    }

    // -----------------------------------------------------------------------
    // UpdateAssignedBookAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAssignedBookAsync_WhenStartDateAfterEndDate_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateAssignedBookAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new UpdateAssignedBookRequest
            {
                StartDateUtc = DateTime.UtcNow.AddDays(5),
                EndDateUtc   = DateTime.UtcNow
            },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("cannot be after end date", result.Error);
    }

    [Fact]
    public async Task UpdateAssignedBookAsync_WhenAssignedBookNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateAssignedBookAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new UpdateAssignedBookRequest { StartDateUtc = DateTime.UtcNow, EndDateUtc = DateTime.UtcNow.AddDays(1) },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Assigned book not found.", result.Error);
    }

    [Fact]
    public async Task UpdateAssignedBookAsync_WhenPointsDeltaZero_UpdatesDatesOnly()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();
        var ab = SeedAssignedBook(db, cls.Id, book.Id, cls: cls, points: 10);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateAssignedBookAsync(
            cls.Id, ab.Id,
            new UpdateAssignedBookRequest
            {
                StartDateUtc = DateTime.UtcNow,
                EndDateUtc   = DateTime.UtcNow.AddDays(30),
                Points       = 10   // same → delta = 0 → skip student adjustment
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(db.StudentPoints.Local);
    }

    [Fact]
    public async Task UpdateAssignedBookAsync_WhenPointsDeltaPositive_SpNull_CreatesStudentPoints()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();
        var ab      = SeedAssignedBook(db, cls.Id, book.Id, cls: cls, points: 10);
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();

        // Completed reading progress so this student enters the loop
        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = ab.Id,
            Status           = ProgressStatus.Completed
        });
        // No StudentPoints → sp is null; no ActivityLog → log is null
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateAssignedBookAsync(
            cls.Id, ab.Id,
            new UpdateAssignedBookRequest
            {
                StartDateUtc = DateTime.UtcNow,
                EndDateUtc   = DateTime.UtcNow.AddDays(30),
                Points       = 20   // delta = +10 > 0 → sp null + delta>0 → creates
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(db.StudentPoints.Local);            // created (sp was null, delta > 0 branch)
        Assert.Equal(10, db.StudentPoints.Local.Single().TotalPoints);
        _badgeServiceMock.Verify(b => b.EvaluateAsync(student.Id, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task UpdateAssignedBookAsync_WhenPointsDeltaPositive_SpExists_LogExists_UpdatesBoth()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();
        var ab      = SeedAssignedBook(db, cls.Id, book.Id, cls: cls, points: 10);
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();

        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = ab.Id,
            Status           = ProgressStatus.Completed
        });
        db.StudentPoints.Add(new StudentPoints
        {
            StudentProfileId = student.Id,
            TotalPoints      = 100
        });
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = student.Id,
            ActivityType     = ActivityType.ReadingProgress,
            ReferenceType    = ActivityReferenceType.AssignedBook,
            ReferenceId      = ab.Id,         // matches filter a.ReferenceId == assignedBookId
            PointsEarned     = 10
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateAssignedBookAsync(
            cls.Id, ab.Id,
            new UpdateAssignedBookRequest
            {
                StartDateUtc = DateTime.UtcNow,
                EndDateUtc   = DateTime.UtcNow.AddDays(30),
                Points       = 15   // delta = +5 → sp not null → updates TotalPoints
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(105, db.StudentPoints.Local.Single().TotalPoints);     // 100 + 5
        Assert.Equal(15, db.ActivityLogs.Local.Single().PointsEarned);      // 10 + 5
    }

    [Fact]
    public async Task UpdateAssignedBookAsync_WhenPointsDeltaNegative_SpNull_SkipsCreation()
    {
        await using var db = CreateDbContext();
        var cls  = SeedClass(db);
        var book = SeedBook(db);
        await db.SaveChangesAsync();
        var ab      = SeedAssignedBook(db, cls.Id, book.Id, cls: cls, points: 20);
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();

        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = ab.Id,
            Status           = ProgressStatus.Completed
        });
        // No StudentPoints → sp=null; delta=-10 → sp null && delta>0 = false, sp not null = false → nothing
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateAssignedBookAsync(
            cls.Id, ab.Id,
            new UpdateAssignedBookRequest
            {
                StartDateUtc = DateTime.UtcNow,
                EndDateUtc   = DateTime.UtcNow.AddDays(30),
                Points       = 10   // delta = -10 < 0, sp=null → neither branch taken
            },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(db.StudentPoints.Local);   // nothing created
    }

    // -----------------------------------------------------------------------
    // RemoveAssignedBookAsync  (SQLite — ExecuteDeleteAsync)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RemoveAssignedBookAsync_WhenAssignedBookNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.RemoveAssignedBookAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Assigned book not found.", result.Error);
    }

    [Fact]
    public async Task RemoveAssignedBookAsync_WhenNoLearningActivities_DeletesAssignedBook()
    {
        var (db, conn) = CreateSqliteDbContext();
        await using (db)
        await using (conn)
        {
            var org  = new Organization { Name = "Org" };
            db.Set<Organization>().Add(org);
            await db.SaveChangesAsync();

            var cls  = new Class { OrganizationId = org.Id, TeacherId = TeacherId, Name = "3A", Grade = 3, SchoolYear = "2024" };
            var book = new Book { Title = "B", Author = "A" };
            db.Classes.Add(cls);
            db.Books.Add(book);
            await db.SaveChangesAsync();

            var ab = new AssignedBook { ClassId = cls.Id, BookId = book.Id };
            db.AssignedBooks.Add(ab);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            // activityIds.Count = 0 → skip learning activity cleanup (false branch)
            var result = await service.RemoveAssignedBookAsync(cls.Id, ab.Id, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, await db.AssignedBooks.CountAsync());
        }
    }

    [Fact]
    public async Task RemoveAssignedBookAsync_WhenHasLearningActivities_DeletesAll()
    {
        var (db, conn) = CreateSqliteDbContext();
        await using (db)
        await using (conn)
        {
            var org  = new Organization { Name = "Org" };
            db.Set<Organization>().Add(org);
            await db.SaveChangesAsync();

            var cls  = new Class { OrganizationId = org.Id, TeacherId = TeacherId, Name = "3A", Grade = 3, SchoolYear = "2024" };
            var book = new Book { Title = "B", Author = "A" };
            db.Classes.Add(cls);
            db.Books.Add(book);
            await db.SaveChangesAsync();

            var ab = new AssignedBook { ClassId = cls.Id, BookId = book.Id };
            db.AssignedBooks.Add(ab);
            await db.SaveChangesAsync();

            // Seed LearningActivity linked to this book → activityIds.Count > 0 (true branch)
            var la = new LearningActivity
            {
                ClassId              = cls.Id,
                CreatedByTeacherId   = TeacherId,
                Title                = "Reading LA",
                Type                 = LearningActivityType.Reading,
                Status               = LearningActivityStatus.Active,
                AssignedBookId       = ab.Id
            };
            db.LearningActivities.Add(la);
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.RemoveAssignedBookAsync(cls.Id, ab.Id, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(0, await db.AssignedBooks.CountAsync());
            Assert.Equal(0, await db.LearningActivities.CountAsync());
        }
    }

    // -----------------------------------------------------------------------
    // UpdateBookAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateBookAsync_WhenBookNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateBookAsync(
            Guid.NewGuid(),
            new BookUpdateRequest { Title = "X", Author = "Y" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task UpdateBookAsync_WhenBookFound_UpdatesFieldsAndReturnsOk()
    {
        await using var db = CreateDbContext();
        var book = SeedBook(db, title: "Old", totalPages: 50);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateBookAsync(
            book.Id,
            new BookUpdateRequest { Title = "New", Author = "NewAuthor", TotalPages = 200 },
            CancellationToken.None);

        Assert.True(result.Success);
        var updated = db.Books.Local.Single();
        Assert.Equal("New", updated.Title);
        Assert.Equal("NewAuthor", updated.Author);
        Assert.Equal(200, updated.TotalPages);
    }
}
