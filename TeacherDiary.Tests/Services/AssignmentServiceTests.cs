using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Assignments;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class AssignmentServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId    = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser>           _currentUserMock       = new();
    private readonly Mock<IActivityService>        _activityMock          = new();
    private readonly Mock<ILearningActivityService> _learningActivityMock = new();
    private readonly Mock<IBadgeService>           _badgeMock             = new();
    private readonly Mock<IEventDispatcher>        _eventDispatcherMock   = new();

    public AssignmentServiceTests()
    {
        _currentUserMock.Setup(u => u.UserId).Returns(TeacherId);
        _currentUserMock.Setup(u => u.OrganizationId).Returns(OrgId);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private AssignmentService CreateService(AppDbContext db)
        => new(db, _currentUserMock.Object, _activityMock.Object,
               _learningActivityMock.Object, _badgeMock.Object, _eventDispatcherMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

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

    private static StudentProfile SeedStudent(AppDbContext db, Guid classId, Guid? parentId = null)
    {
        var student = new StudentProfile
        {
            ClassId = classId,
            FirstName = "Student",
            LastName = "Test",
            ParentId = parentId
        };
        db.Students.Add(student);
        return student;
    }

    private static Assignment SeedAssignment(
        AppDbContext db, Guid classId, int points = 10, DateTime? dueDate = null)
    {
        var assignment = new Assignment
        {
            ClassId = classId,
            CreatedByTeacherId = TeacherId,
            Title = "Test Assignment",
            Points = points,
            DueDate = dueDate
        };
        db.Assignments.Add(assignment);
        return assignment;
    }

    private static AssignmentProgress SeedProgress(
        AppDbContext db,
        Guid studentId,
        Assignment assignment,
        ProgressStatus status,
        DateTime? startedAt = null)
    {
        var progress = new AssignmentProgress
        {
            StudentProfileId = studentId,
            AssignmentId = assignment.Id,
            Assignment = assignment,
            Status = status,
            StartedAt = startedAt
        };
        db.AssignmentProgress.Add(progress);
        return progress;
    }

    private static AssignmentCreateRequest MakeCreateRequest(string title = "Homework") => new()
    {
        Title = title,
        Description = "Do it",
        Subject = "Math",
        DueDate = null,
        Points = 10
    };

    // -----------------------------------------------------------------------
    // CreateAssignmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAssignmentAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateAssignmentAsync(
            Guid.NewGuid(), MakeCreateRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task CreateAssignmentAsync_WhenClassFoundWithNoStudents_ReturnsOkAndDispatchesEvent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.CreateAssignmentAsync(
            cls.Id, MakeCreateRequest("Homework 1"), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);

        _learningActivityMock.Verify(
            l => l.CreateForAssignmentAsync(It.IsAny<Assignment>(), CancellationToken.None),
            Times.Once);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<AssignmentCreatedEvent>(ev => ev.ClassId == cls.Id && ev.Title == "Homework 1"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CreateAssignmentAsync_WhenClassFoundWithStudents_CreatesProgressRowPerStudent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.CreateAssignmentAsync(cls.Id, MakeCreateRequest(), CancellationToken.None);

        Assert.Equal(2, db.AssignmentProgress.Count());
    }

    // -----------------------------------------------------------------------
    // UpdateProgressAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateProgressAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateProgressAsync(
            Guid.NewGuid(), Guid.NewGuid(), true, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenParentIdMismatch_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: Guid.NewGuid()); // different from currentUser.UserId
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateProgressAsync(
            student.Id, Guid.NewGuid(), true, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Forbidden.", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenProgressNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateProgressAsync(
            student.Id, Guid.NewGuid(), true, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Progress not found.", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenDeadlineHasPassed_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id, dueDate: DateTime.UtcNow.AddDays(-1));
        SeedProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateProgressAsync(
            student.Id, assignment.Id, true, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Deadline has passed", result.Error);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenNoDueDate_Succeeds()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id, dueDate: null);
        SeedProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateProgressAsync(
            student.Id, assignment.Id, false, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenDeadlineIsInFuture_Succeeds()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id, dueDate: DateTime.UtcNow.AddDays(7));
        SeedProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateProgressAsync(
            student.Id, assignment.Id, false, CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenStartedAtIsNull_SetsStartedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id);
        var progress = SeedProgress(db, student.Id, assignment, ProgressStatus.NotStarted, startedAt: null);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateProgressAsync(student.Id, assignment.Id, false, CancellationToken.None);

        Assert.NotNull(progress.StartedAt);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenStartedAtAlreadySet_PreservesStartedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id);
        var originalStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var progress = SeedProgress(db, student.Id, assignment, ProgressStatus.InProgress, startedAt: originalStart);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateProgressAsync(student.Id, assignment.Id, false, CancellationToken.None);

        Assert.Equal(originalStart, progress.StartedAt);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenCompletedForFirstTime_SetsCompletedAtLogsActivityAndDispatchesEvent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id, points: 20);
        var progress = SeedProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateProgressAsync(student.Id, assignment.Id, true, CancellationToken.None);

        Assert.Equal(ProgressStatus.Completed, progress.Status);
        Assert.NotNull(progress.CompletedAt);

        _activityMock.Verify(
            a => a.LogAssignmentCompletedAsync(student.Id, assignment.Id, 20, CancellationToken.None),
            Times.Once);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(
                It.Is<AssignmentCompletedEvent>(ev =>
                    ev.StudentId == student.Id && ev.AssignmentId == assignment.Id),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenAlreadyCompleted_DoesNotLogActivityAgain()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateProgressAsync(student.Id, assignment.Id, true, CancellationToken.None);

        _activityMock.Verify(
            a => a.LogAssignmentCompletedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(It.IsAny<AssignmentCompletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateProgressAsync_WhenSetToInProgress_SetsStatusAndDoesNotDispatchEvent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, parentId: TeacherId);
        var assignment = SeedAssignment(db, cls.Id);
        var progress = SeedProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateProgressAsync(
            student.Id, assignment.Id, false, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(ProgressStatus.InProgress, progress.Status);

        _eventDispatcherMock.Verify(
            e => e.PublishAsync(It.IsAny<AssignmentCompletedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // GetAssignmentsByClassAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAssignmentsByClassAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetAssignmentsByClassAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetAssignmentsByClassAsync_WhenClassFound_ReturnsAssignments()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        SeedAssignment(db, cls.Id, points: 15, dueDate: DateTime.UtcNow.AddDays(5));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetAssignmentsByClassAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        var item = Assert.Single(result.Data);
        Assert.Equal("Test Assignment", item.Title);
        Assert.Equal(15, item.Points);
        Assert.False(item.IsExpired);
    }

    [Fact]
    public async Task GetAssignmentsByClassAsync_WhenAssignmentDueDatePassed_IsExpiredTrue()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        SeedAssignment(db, cls.Id, dueDate: DateTime.UtcNow.AddDays(-2));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetAssignmentsByClassAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        var item = Assert.Single(result.Data);
        Assert.True(item.IsExpired);
    }

    // -----------------------------------------------------------------------
    // GetStudentProgressForAssignmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetStudentProgressForAssignmentAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentProgressForAssignmentAsync(
            Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetStudentProgressForAssignmentAsync_WhenClassFound_ReturnsStudentProgress()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetStudentProgressForAssignmentAsync(
            cls.Id, assignment.Id, CancellationToken.None);

        Assert.True(result.Success);
        var item = Assert.Single(result.Data);
        Assert.Equal(student.Id, item.StudentId);
        Assert.Equal(ProgressStatus.Completed, item.Status);
    }

    // -----------------------------------------------------------------------
    // UpdateAssignmentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAssignmentAsync_WhenAssignmentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateAssignmentAsync(
            Guid.NewGuid(), Guid.NewGuid(),
            new AssignmentUpdateRequest { Title = "X", Points = 5 },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Assignment not found.", result.Error);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenPointsDeltaIsZero_UpdatesFieldsOnly()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "Updated", Subject = "Science", Points = 10 },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Updated", assignment.Title);
        Assert.Equal("Science", assignment.Subject);
        _badgeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenPositiveDeltaAndNoCompletedStudents_SkipsPointsLoop()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        SeedProgress(db, student.Id, assignment, ProgressStatus.InProgress);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 20 },
            CancellationToken.None);

        Assert.True(result.Success);
        _badgeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenPositiveDelta_StudentHasNoPoints_CreatesStudentPoints()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 20 }, // delta = +10
            CancellationToken.None);

        var sp = db.StudentPoints.Single();
        Assert.Equal(student.Id, sp.StudentProfileId);
        Assert.Equal(10, sp.TotalPoints);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenPositiveDelta_StudentHasExistingPoints_IncreasesTotalPoints()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = student.Id, TotalPoints = 30 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 15 }, // delta = +5
            CancellationToken.None);

        var sp = db.StudentPoints.Single();
        Assert.Equal(35, sp.TotalPoints);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenNegativeDelta_StudentHasExistingPoints_DecreasesTotalPoints()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 20);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = student.Id, TotalPoints = 50 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 10 }, // delta = -10
            CancellationToken.None);

        var sp = db.StudentPoints.Single();
        Assert.Equal(40, sp.TotalPoints);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenNegativeDelta_StudentHasNoPoints_DoesNotCreateStudentPoints()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 20);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 10 }, // delta = -10, sp is null
            CancellationToken.None);

        Assert.Empty(db.StudentPoints.ToList());
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenActivityLogExists_UpdatesLogPointsEarned()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = student.Id, TotalPoints = 10 });
        var log = new ActivityLog
        {
            StudentProfileId = student.Id,
            ActivityType = ActivityType.AssignmentCompleted,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = assignment.Id,
            PointsEarned = 10
        };
        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 15 }, // delta = +5
            CancellationToken.None);

        Assert.Equal(15, log.PointsEarned);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenNoActivityLog_DoesNotCrash()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        SeedProgress(db, student.Id, assignment, ProgressStatus.Completed);
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = student.Id, TotalPoints = 10 });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 15 }, // delta = +5, no log
            CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_WhenPointsDeltaExists_CallsBadgeEvaluateForEachCompletedStudent()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student1 = SeedStudent(db, cls.Id);
        var student2 = SeedStudent(db, cls.Id);
        var assignment = SeedAssignment(db, cls.Id, points: 10);
        SeedProgress(db, student1.Id, assignment, ProgressStatus.Completed);
        SeedProgress(db, student2.Id, assignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.UpdateAssignmentAsync(
            cls.Id, assignment.Id,
            new AssignmentUpdateRequest { Title = "X", Points = 20 }, // delta = +10
            CancellationToken.None);

        _badgeMock.Verify(
            b => b.EvaluateAsync(It.IsAny<Guid>(), CancellationToken.None),
            Times.Exactly(2));
    }
}
