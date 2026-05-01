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

public class StudentServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId     = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser>     _currentUserMock     = new();
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();

    public StudentServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private StudentService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object, _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static Class SeedClass(AppDbContext db, Guid? teacherId = null, Guid? orgId = null)
    {
        var cls = new Class
        {
            OrganizationId = orgId     ?? OrgId,
            TeacherId      = teacherId ?? TeacherId,
            Name           = "3A",
            Grade          = 3,
            SchoolYear     = "2024/2025"
        };
        db.Classes.Add(cls);
        return cls;
    }

    private static StudentProfile SeedStudent(AppDbContext db, Guid? classId = null,
        string firstName = "Alice", string lastName = "Smith")
    {
        var s = new StudentProfile { ClassId = classId, FirstName = firstName, LastName = lastName };
        db.Students.Add(s);
        return s;
    }

    // -----------------------------------------------------------------------
    // GetByClassAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetByClassAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetByClassAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetByClassAsync_WhenStudentHasStreakAndPoints_SetsBothMedalCodes()
    {
        await using var db = CreateDbContext();
        var cls     = SeedClass(db);
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();

        // BestStreak=10 → non-null medal; PointsEarned=500 → non-null medal
        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = student.Id, CurrentStreak = 10, BestStreak = 10
        });
        db.ActivityLogs.Add(new ActivityLog
        {
            StudentProfileId = student.Id,
            ActivityType     = ActivityType.ReadingProgress,
            ReferenceType    = ActivityReferenceType.AssignedBook,
            ReferenceId      = Guid.NewGuid(),
            PointsEarned     = 500
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetByClassAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.NotNull(result.Data[0].TopMedalCode);       // TryGetValue streak → true
        Assert.NotNull(result.Data[0].TopPointsMedalCode); // TryGetValue points → true
    }

    [Fact]
    public async Task GetByClassAsync_WhenStudentHasNoStreakOrPoints_MedalCodesAreNull()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetByClassAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Null(result.Data[0].TopMedalCode);          // TryGetValue streak → false
        Assert.Null(result.Data[0].TopPointsMedalCode);    // TryGetValue points → false
    }

    // -----------------------------------------------------------------------
    // AddStudentToClassAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddStudentToClassAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.AddStudentToClassAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task AddStudentToClassAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.AddStudentToClassAsync(
            cls.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student not found.", result.Error);
    }

    [Fact]
    public async Task AddStudentToClassAsync_WhenValid_CreatesAllProgressRowsAndPublishesEvent()
    {
        await using var db = CreateDbContext();
        var cls     = SeedClass(db);
        var student = SeedStudent(db);   // ClassId=null initially
        await db.SaveChangesAsync();

        // Seed one of each class item the student should get progress for
        var book = new Book { Title = "B", Author = "A", TotalPages = 100 };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var ab = new AssignedBook { ClassId = cls.Id, BookId = book.Id, Book = book };
        db.AssignedBooks.Add(ab);

        var assignment = new Assignment
        {
            ClassId = cls.Id, CreatedByTeacherId = TeacherId,
            Title = "A1", Description = "D"
        };
        db.Assignments.Add(assignment);

        var challenge = new Challenge
        {
            ClassId = cls.Id, Title = "C1", Description = "D",
            TargetValue = 5, EndDate = DateTime.UtcNow.AddDays(7)
        };
        db.Challenges.Add(challenge);

        var activeLa = new LearningActivity
        {
            ClassId = cls.Id, CreatedByTeacherId = TeacherId,
            Title = "LA Active", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            IsActive = true, TargetValue = 50
        };
        var inactiveLa = new LearningActivity
        {
            ClassId = cls.Id, CreatedByTeacherId = TeacherId,
            Title = "LA Inactive", Type = LearningActivityType.Reading,
            Status = LearningActivityStatus.Active,
            IsActive = false  // should be excluded from progress rows
        };
        db.LearningActivities.Add(activeLa);
        db.LearningActivities.Add(inactiveLa);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.AddStudentToClassAsync(cls.Id, student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);

        // Student ClassId set
        Assert.Equal(cls.Id, db.Students.Local.Single().ClassId);

        // One ReadingProgress for the assigned book
        Assert.Single(db.ReadingProgress.Local);

        // One AssignmentProgress
        Assert.Single(db.AssignmentProgress.Local);

        // One ChallengeProgress
        Assert.Single(db.ChallengeProgress.Local);

        // One StudentLearningActivityProgress (only for active LA)
        Assert.Single(db.StudentLearningActivityProgress.Local);
        Assert.Equal(activeLa.Id, db.StudentLearningActivityProgress.Local.Single().LearningActivityId);

        _eventDispatcherMock.Verify(
            d => d.PublishAsync(It.IsAny<StudentJoinedClassEvent>(), CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task AddStudentToClassAsync_WhenStudentAlreadyHasProgress_SkipsDuplicateRows()
    {
        await using var db = CreateDbContext();
        var cls     = SeedClass(db);
        var student = SeedStudent(db);
        await db.SaveChangesAsync();

        var book = new Book { Title = "B", Author = "A" };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var ab = new AssignedBook { ClassId = cls.Id, BookId = book.Id, Book = book };
        db.AssignedBooks.Add(ab);
        await db.SaveChangesAsync();

        // Student already has a ReadingProgress for this book → should be excluded
        db.ReadingProgress.Add(new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId   = ab.Id
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.AddStudentToClassAsync(cls.Id, student.Id, CancellationToken.None);

        Assert.True(result.Success);
        // Still only 1 ReadingProgress (the pre-existing one, not duplicated)
        Assert.Equal(1, db.ReadingProgress.Count());
    }

    // -----------------------------------------------------------------------
    // SearchAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SearchAsync_WhenStudentClassIdIsNull_IncludesInResults()
    {
        await using var db = CreateDbContext();
        // ClassId=null → always included regardless of org (first part of OR)
        SeedStudent(db, classId: null, firstName: "Alice");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SearchAsync("alice", 1, 10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_WhenStudentClassIdMatchesOrg_IncludesInResults()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db, orgId: OrgId);  // same org as currentUser
        SeedStudent(db, classId: cls.Id, firstName: "Bob");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SearchAsync("bob", 1, 10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_WhenStudentClassIdBelongsToDifferentOrg_ExcludesFromResults()
    {
        await using var db = CreateDbContext();
        var otherOrg = Guid.NewGuid();
        var cls = SeedClass(db, orgId: otherOrg); // different org → excluded
        SeedStudent(db, classId: cls.Id, firstName: "Carol");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SearchAsync("carol", 1, 10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0, result.Data.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_WhenNameMatchesByLastName_IncludesInResults()
    {
        await using var db = CreateDbContext();
        SeedStudent(db, firstName: "Dave", lastName: "Johnson");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        // firstName does NOT match "johnson" but lastName does
        var result = await service.SearchAsync("johnson", 1, 10, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data.TotalCount);
        Assert.Equal("Johnson", result.Data.Items[0].LastName);
    }

    [Fact]
    public async Task SearchAsync_WhenPageIsTwo_SkipsFirstPageItems()
    {
        await using var db = CreateDbContext();
        for (var i = 0; i < 3; i++)
            SeedStudent(db, firstName: $"Student{i}", lastName: "Test");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.SearchAsync("test", page: 2, pageSize: 2, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(3, result.Data.TotalCount);
        Assert.Single(result.Data.Items);   // 3 total, skip 2 → 1 on page 2
    }

    // -----------------------------------------------------------------------
    // RemoveStudentFromClassAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RemoveStudentFromClassAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.RemoveStudentFromClassAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task RemoveStudentFromClassAsync_WhenStudentNotInClass_ReturnsFail()
    {
        await using var db = CreateDbContext();
        SeedStudent(db, classId: null);  // ClassId is null → "not assigned"
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.RemoveStudentFromClassAsync(
            db.Students.Local.Single().Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Student is not assigned to a class.", result.Error);
    }

    [Fact]
    public async Task RemoveStudentFromClassAsync_WhenClassBelongsToOtherTeacher_ReturnsForbidden()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db, teacherId: Guid.NewGuid()); // different teacher
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.RemoveStudentFromClassAsync(student.Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Forbidden.", result.Error);
    }

    [Fact]
    public async Task RemoveStudentFromClassAsync_WhenValid_SetsClassIdNullAndReturnsOk()
    {
        await using var db = CreateDbContext();
        var cls     = SeedClass(db);
        var student = SeedStudent(db, classId: cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.RemoveStudentFromClassAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Null(db.Students.Local.Single().ClassId);
    }
}
