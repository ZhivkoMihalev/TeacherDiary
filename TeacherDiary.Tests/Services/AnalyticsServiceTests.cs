using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class AnalyticsServiceTests
{
    private static readonly Guid OrgId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public AnalyticsServiceTests()
    {
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private AnalyticsService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object);

    private static Class SeedClass(AppDbContext db, bool isDeleted = false)
    {
        var cls = new Class
        {
            OrganizationId = OrgId,
            TeacherId = Guid.NewGuid(),
            Name = "3А",
            Grade = 3,
            SchoolYear = "2025/2026"
        };
        if (isDeleted)
            cls.IsDeleted = true;
        db.Classes.Add(cls);
        return cls;
    }

    private static StudentProfile SeedStudent(AppDbContext db, Guid classId)
    {
        var s = new StudentProfile
        {
            ClassId = classId,
            FirstName = "Alice",
            LastName = "Smith",
            IsActive = true
        };
        db.Students.Add(s);
        return s;
    }

    private static ActivityLog SeedLog(
        AppDbContext db,
        Guid studentId,
        ActivityType type,
        DateOnly date,
        int? pagesRead = null,
        int? pointsEarned = null)
    {
        var log = new ActivityLog
        {
            StudentProfileId = studentId,
            ActivityType = type,
            ReferenceType = ActivityReferenceType.AssignedBook,
            ReferenceId = Guid.NewGuid(),
            Date = date,
            PagesRead = pagesRead,
            PointsEarned = pointsEarned
        };
        db.ActivityLogs.Add(log);
        return log;
    }

    private static Assignment SeedAssignment(AppDbContext db, Guid classId, string subject = "Math")
    {
        var a = new Assignment
        {
            ClassId = classId,
            CreatedByTeacherId = Guid.NewGuid(),
            Title = "HW",
            Description = "Desc",
            Subject = subject
        };
        db.Assignments.Add(a);
        return a;
    }

    private static AssignmentProgress SeedAssignmentProgress(
        AppDbContext db,
        Guid studentId,
        Assignment assignment,
        ProgressStatus status = ProgressStatus.NotStarted)
    {
        var ap = new AssignmentProgress
        {
            StudentProfileId = studentId,
            AssignmentId = assignment.Id,
            Assignment = assignment,
            Status = status
        };
        db.AssignmentProgress.Add(ap);
        return ap;
    }

    private static ReadingProgress SeedReadingProgress(
        AppDbContext db,
        Guid studentId,
        Guid assignedBookId,
        ProgressStatus status = ProgressStatus.NotStarted)
    {
        var rp = new ReadingProgress
        {
            StudentProfileId = studentId,
            AssignedBookId = assignedBookId,
            Status = status
        };
        db.ReadingProgress.Add(rp);
        return rp;
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenDaysIsZero_ClampsTo30()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 0, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(30, result.Data.ActivityTimeline.Count);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenDaysIsNegative_ClampsTo30()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, -5, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(30, result.Data.ActivityTimeline.Count);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenDaysExceeds90_ClampsTo90()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 200, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(90, result.Data.ActivityTimeline.Count);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenClassDoesNotExist_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(Guid.NewGuid(), 7, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenClassBelongsToDifferentOrg_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = new Class
        {
            OrganizationId = Guid.NewGuid(),
            TeacherId = Guid.NewGuid(),
            Name = "5B",
            Grade = 5,
            SchoolYear = "2025/2026"
        };
        db.Classes.Add(cls);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenClassIsSoftDeleted_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db, isDeleted: true);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Class not found.", result.Error);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenNoStudents_ReturnsZeroFilledTimeline()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(7, result.Data.ActivityTimeline.Count);
        Assert.All(result.Data.ActivityTimeline, d =>
        {
            Assert.Equal(0, d.ActiveStudents);
            Assert.Equal(0, d.PagesRead);
            Assert.Equal(0, d.PointsEarned);
            Assert.Equal(0, d.CompletedAssignments);
        });
        Assert.Empty(result.Data.StudentEngagement);
        Assert.Empty(result.Data.SubjectCompletion);
        Assert.Equal(0, result.Data.ReadingStats.NotStartedCount);
        Assert.Equal(0, result.Data.ReadingStats.InProgressCount);
        Assert.Equal(0, result.Data.ReadingStats.CompletedCount);
        Assert.Equal(0, result.Data.ReadingStats.PagesReadLast7Days);
        Assert.Equal(0, result.Data.ReadingStats.PagesReadLast30Days);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenActivityOnDay_TimelineAggregatesCorrectly()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student1 = SeedStudent(db, cls.Id);
        var student2 = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student1.Id, ActivityType.ReadingProgress, today, pagesRead: 10, pointsEarned: 5);
        SeedLog(db, student1.Id, ActivityType.AssignmentCompleted, today, pagesRead: null, pointsEarned: 10);
        SeedLog(db, student2.Id, ActivityType.ReadingProgress, today, pagesRead: 20, pointsEarned: 8);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var todaySlot = result.Data.ActivityTimeline.Single(d => d.Date == today);
        Assert.Equal(2, todaySlot.ActiveStudents);
        Assert.Equal(30, todaySlot.PagesRead);
        Assert.Equal(23, todaySlot.PointsEarned);
        Assert.Equal(1, todaySlot.CompletedAssignments);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenDayHasNoActivity_TimelineSlotIsAllZero()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today, pagesRead: 5, pointsEarned: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var yesterdaySlot = result.Data.ActivityTimeline.Single(d => d.Date == yesterday);
        Assert.Equal(0, yesterdaySlot.ActiveStudents);
        Assert.Equal(0, yesterdaySlot.PagesRead);
        Assert.Equal(0, yesterdaySlot.PointsEarned);
        Assert.Equal(0, yesterdaySlot.CompletedAssignments);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenPagesReadIsNull_TreatedAsZeroInTimeline()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.AssignmentCompleted, today, pagesRead: null, pointsEarned: 10);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var todaySlot = result.Data.ActivityTimeline.Single(d => d.Date == today);
        Assert.Equal(0, todaySlot.PagesRead);
        Assert.Equal(10, todaySlot.PointsEarned);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenMultipleActivityTypesOnSameDay_AllAggregated()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today, pagesRead: 5, pointsEarned: 2);
        SeedLog(db, student.Id, ActivityType.AssignmentCompleted, today, pagesRead: null, pointsEarned: 10);
        SeedLog(db, student.Id, ActivityType.ChallengeCompleted, today, pagesRead: null, pointsEarned: 15);
        SeedLog(db, student.Id, ActivityType.AssignmentCompleted, today, pagesRead: null, pointsEarned: 10);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var todaySlot = result.Data.ActivityTimeline.Single(d => d.Date == today);
        Assert.Equal(1, todaySlot.ActiveStudents);
        Assert.Equal(5, todaySlot.PagesRead);
        Assert.Equal(37, todaySlot.PointsEarned);
        Assert.Equal(2, todaySlot.CompletedAssignments);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasRecentActivity_EngagementReflectsIt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today, pagesRead: 5, pointsEarned: 3);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today.AddDays(-1), pagesRead: 5, pointsEarned: 3);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today.AddDays(-2), pagesRead: 5, pointsEarned: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(3, eng.DaysActiveLast7);
        Assert.True(eng.ActiveToday);
        Assert.NotNull(eng.LastActivityAt);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasNoRecentActivity_EngagementIsZero()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(0, eng.DaysActiveLast7);
        Assert.False(eng.ActiveToday);
        Assert.Null(eng.LastActivityAt);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasNoPointsRecord_TotalPointsIsZero()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(0, eng.TotalPoints);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasPointsRecord_TotalPointsIsCorrect()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        db.StudentPoints.Add(new StudentPoints
        {
            StudentProfileId = student.Id,
            TotalPoints = 250
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(250, eng.TotalPoints);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasNoStreakRecord_CurrentStreakIsZero()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(0, eng.CurrentStreak);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasStreakRecord_CurrentStreakIsCorrect()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        db.StudentStreaks.Add(new StudentStreak
        {
            StudentProfileId = student.Id,
            CurrentStreak = 12,
            BestStreak = 15
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(12, eng.CurrentStreak);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenMultipleStudents_EngagementOrderedByTotalPointsDescending()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var studentA = SeedStudent(db, cls.Id);
        var studentB = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        db.StudentPoints.Add(new StudentPoints { StudentProfileId = studentA.Id, TotalPoints = 50 });
        db.StudentPoints.Add(new StudentPoints { StudentProfileId = studentB.Id, TotalPoints = 200 });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.StudentEngagement.Count);
        Assert.Equal(studentB.Id, result.Data.StudentEngagement[0].StudentId);
        Assert.Equal(studentA.Id, result.Data.StudentEngagement[1].StudentId);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenActiveTodayLog_ActiveTodayIsTrue()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today, pagesRead: 1, pointsEarned: 1);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data.StudentEngagement[0].ActiveToday);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenLogOnlyOutsideSevenDays_DaysActiveLast7IsZeroActiveTodayIsFalse()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today.AddDays(-20), pagesRead: 5, pointsEarned: 3);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 30, CancellationToken.None);

        Assert.True(result.Success);
        var eng = Assert.Single(result.Data.StudentEngagement);
        Assert.Equal(0, eng.DaysActiveLast7);
        Assert.False(eng.ActiveToday);
        Assert.NotNull(eng.LastActivityAt);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenDaysIs7_PagesReadLast30DaysReflectsFullThirtyDayWindow()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today.AddDays(-15), pagesRead: 40, pointsEarned: 5);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today.AddDays(-3), pagesRead: 10, pointsEarned: 5);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(50, result.Data.ReadingStats.PagesReadLast30Days);
        Assert.Equal(10, result.Data.ReadingStats.PagesReadLast7Days);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenSubjectHasSomeCompleted_CompletionRateIsCorrect()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var assignment = SeedAssignment(db, cls.Id, "Science");
        await db.SaveChangesAsync();
        SeedAssignmentProgress(db, student.Id, assignment, ProgressStatus.Completed);
        SeedAssignmentProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var sub = Assert.Single(result.Data.SubjectCompletion);
        Assert.Equal("Science", sub.Subject);
        Assert.Equal(2, sub.TotalRows);
        Assert.Equal(1, sub.CompletedRows);
        Assert.Equal(0.5, sub.CompletionRate, 3);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenSubjectHasNoneCompleted_CompletionRateIsZero()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var assignment = SeedAssignment(db, cls.Id, "History");
        await db.SaveChangesAsync();
        SeedAssignmentProgress(db, student.Id, assignment, ProgressStatus.NotStarted);
        SeedAssignmentProgress(db, student.Id, assignment, ProgressStatus.InProgress);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var sub = Assert.Single(result.Data.SubjectCompletion);
        Assert.Equal(0.0, sub.CompletionRate, 3);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenReadingProgressExists_StatusCountsAreCorrect()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var book = new Book { Title = "Book1", Author = "Auth" };
        db.Books.Add(book);
        await db.SaveChangesAsync();

        var ab1 = new AssignedBook { ClassId = cls.Id, BookId = book.Id };
        var ab2 = new AssignedBook { ClassId = cls.Id, BookId = book.Id };
        var ab3 = new AssignedBook { ClassId = cls.Id, BookId = book.Id };
        db.AssignedBooks.AddRange(ab1, ab2, ab3);
        await db.SaveChangesAsync();

        SeedReadingProgress(db, student.Id, ab1.Id, ProgressStatus.NotStarted);
        SeedReadingProgress(db, student.Id, ab2.Id, ProgressStatus.InProgress);
        SeedReadingProgress(db, student.Id, ab3.Id, ProgressStatus.Completed);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, result.Data.ReadingStats.NotStartedCount);
        Assert.Equal(1, result.Data.ReadingStats.InProgressCount);
        Assert.Equal(1, result.Data.ReadingStats.CompletedCount);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenStudentHasActivityOnAllSevenDays_DaysActiveLast7IsSeven()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 7; i++)
            SeedLog(db, student.Id, ActivityType.ReadingProgress, today.AddDays(-i), pagesRead: 1, pointsEarned: 1);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(7, result.Data.StudentEngagement[0].DaysActiveLast7);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenTwoStudentsWithSameDayActivity_ActiveStudentsCountIsDistinct()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student1 = SeedStudent(db, cls.Id);
        var student2 = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student1.Id, ActivityType.ReadingProgress, today, pagesRead: 5, pointsEarned: 2);
        SeedLog(db, student1.Id, ActivityType.AssignmentCompleted, today, pagesRead: null, pointsEarned: 5);
        SeedLog(db, student2.Id, ActivityType.ReadingProgress, today, pagesRead: 3, pointsEarned: 1);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        var todaySlot = result.Data.ActivityTimeline.Single(d => d.Date == today);
        Assert.Equal(2, todaySlot.ActiveStudents);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenMultipleSubjects_SubjectCompletionOrderedBySubject()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var mathAssignment = SeedAssignment(db, cls.Id, "Math");
        var artAssignment = SeedAssignment(db, cls.Id, "Art");
        await db.SaveChangesAsync();
        SeedAssignmentProgress(db, student.Id, mathAssignment, ProgressStatus.Completed);
        SeedAssignmentProgress(db, student.Id, artAssignment, ProgressStatus.NotStarted);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.SubjectCompletion.Count);
        Assert.Equal("Art", result.Data.SubjectCompletion[0].Subject);
        Assert.Equal("Math", result.Data.SubjectCompletion[1].Subject);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenPagesReadLast7DaysHasOnlyReadingProgressType_OtherTypesExcluded()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student.Id, ActivityType.ReadingProgress, today, pagesRead: 20, pointsEarned: 5);
        SeedLog(db, student.Id, ActivityType.AssignmentCompleted, today, pagesRead: 50, pointsEarned: 10);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(20, result.Data.ReadingStats.PagesReadLast7Days);
    }

    [Fact]
    public async Task GetClassAnalyticsAsync_WhenDeletedAssignmentExists_NotIncludedInSubjectCompletion()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var student = SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();

        var deletedAssignment = SeedAssignment(db, cls.Id, "DeletedSubject");
        await db.SaveChangesAsync();
        deletedAssignment.IsDeleted = true;
        SeedAssignmentProgress(db, student.Id, deletedAssignment, ProgressStatus.Completed);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassAnalyticsAsync(cls.Id, 7, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data.SubjectCompletion);
    }
}
