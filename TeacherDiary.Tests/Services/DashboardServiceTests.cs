using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class DashboardServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public DashboardServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private DashboardService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object);

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
        AppDbContext db, Guid classId,
        string firstName = "Alice", string lastName = "Smith",
        bool isActive = true)
    {
        var s = new StudentProfile
        {
            ClassId = classId,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive
        };
        db.Students.Add(s);
        return s;
    }

    private static ActivityLog SeedLog(
        AppDbContext db, StudentProfile student, ActivityType type,
        DateOnly? date = null, int? pagesRead = null, int? pointsEarned = null,
        DateTime? createdAt = null)
    {
        var log = new ActivityLog
        {
            StudentProfileId = student.Id,
            ActivityType = type,
            ReferenceType = ActivityReferenceType.Assignment,
            ReferenceId = Guid.NewGuid(),
            Date = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            PagesRead = pagesRead,
            PointsEarned = pointsEarned,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        db.ActivityLogs.Add(log);
        return log;
    }

    private static StudentStreak SeedStreak(
        AppDbContext db, StudentProfile student, int best = 7, int current = 3)
    {
        var streak = new StudentStreak
        {
            StudentProfileId = student.Id,
            StudentProfile = student,
            BestStreak = best,
            CurrentStreak = current,
            LastActiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        db.StudentStreaks.Add(streak);
        return streak;
    }

    private static (Badge badge, StudentBadge sb) SeedStudentBadge(
        AppDbContext db, StudentProfile student, DateTime? awardedAt = null)
    {
        var badge = new Badge
        {
            Code = "FIRST_BOOK",
            Name = "First Book",
            Description = "Completed first book",
            Icon = "book"
        };
        db.Badges.Add(badge);
        var sb = new StudentBadge
        {
            StudentProfileId = student.Id,
            StudentProfile = student,
            BadgeId = badge.Id,
            Badge = badge,
            AwardedAt = awardedAt ?? DateTime.UtcNow
        };
        db.StudentBadges.Add(sb);
        return (badge, sb);
    }

    private static LearningActivity SeedLearningActivity(
        AppDbContext db, Guid classId, bool isActive = true, DateTime? dueDate = null)
    {
        var la = new LearningActivity
        {
            ClassId = classId,
            CreatedByTeacherId = TeacherId,
            Type = LearningActivityType.Assignment,
            Title = "Test Learning Activity",
            IsActive = isActive,
            DueDateUtc = dueDate
        };
        db.LearningActivities.Add(la);
        return la;
    }

    private static StudentLearningActivityProgress SeedLearningActivityProgress(
        AppDbContext db, StudentProfile student, LearningActivity la,
        ProgressStatus status = ProgressStatus.NotStarted, DateTime? completedAt = null)
    {
        var p = new StudentLearningActivityProgress
        {
            LearningActivityId = la.Id,
            LearningActivity = la,
            StudentProfileId = student.Id,
            StudentProfile = student,
            Status = status,
            CompletedAt = completedAt
        };
        db.StudentLearningActivityProgress.Add(p);
        return p;
    }

    private static (Book book, AssignedBook ab) SeedAssignedBook(
        AppDbContext db, Guid classId, DateTime? endDateUtc = null)
    {
        var book = new Book { Title = "Test Book", Author = "Author" };
        db.Books.Add(book);
        var ab = new AssignedBook
        {
            ClassId = classId,
            BookId = book.Id,
            Book = book,
            EndDateUtc = endDateUtc
        };
        db.AssignedBooks.Add(ab);
        return (book, ab);
    }

    private static ReadingProgress SeedReadingProgress(
        AppDbContext db, StudentProfile student, AssignedBook ab)
    {
        var rp = new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId = ab.Id,
            AssignedBook = ab,
            CurrentPage = 50,
            TotalPages = 200,
            Status = ProgressStatus.InProgress
        };
        db.ReadingProgress.Add(rp);
        return rp;
    }

    private static Assignment SeedAssignment(
        AppDbContext db, Guid classId, DateTime? dueDate = null)
    {
        var a = new Assignment
        {
            ClassId = classId,
            CreatedByTeacherId = TeacherId,
            Title = "Test Assignment",
            Subject = "Math",
            DueDate = dueDate
        };
        db.Assignments.Add(a);
        return a;
    }

    private static AssignmentProgress SeedAssignmentProgress(
        AppDbContext db, StudentProfile student, Assignment assignment)
    {
        var ap = new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId = assignment.Id,
            Assignment = assignment,
            Status = ProgressStatus.InProgress
        };
        db.AssignmentProgress.Add(ap);
        return ap;
    }

    private static Challenge SeedChallenge(AppDbContext db, Class cls, DateTime? endDate = null)
    {
        var c = new Challenge
        {
            ClassId = cls.Id,
            Class = cls,
            Title = "Test Challenge",
            TargetValue = 5,
            Points = 50,
            EndDate = endDate ?? DateTime.UtcNow.AddDays(30)
        };
        db.Challenges.Add(c);
        return c;
    }

    private static ChallengeProgress SeedChallengeProgress(
        AppDbContext db, StudentProfile student, Challenge challenge,
        bool completed = false, DateTime? startedAt = null)
    {
        var cp = new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = challenge.Id,
            Challenge = challenge,
            StudentProfile = student,
            Completed = completed,
            StartedAt = startedAt
        };
        db.ChallengeProgress.Add(cp);
        return cp;
    }

[Fact]
    public async Task GetClassDashboardAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetClassDashboardAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetClassDashboardAsync_WhenClassFound_ReturnsDashboardWithCorrectData()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student1 = SeedStudent(db, cls.Id, "Alice", "Smith");
        var student2 = SeedStudent(db, cls.Id, "Bob", "Jones");
        SeedStudent(db, cls.Id, "Inactive", "User", isActive: false);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var recent = today.AddDays(-3);

        SeedLog(db, student1, ActivityType.ReadingProgress, today, pagesRead: 20, pointsEarned: 10);
        SeedLog(db, student1, ActivityType.AssignmentCompleted, recent, pointsEarned: 5);
        SeedLog(db, student2, ActivityType.ReadingProgress, recent, pagesRead: 30, pointsEarned: 15);

        SeedStreak(db, student1, best: 7, current: 3);

        var la = SeedLearningActivity(db, cls.Id, isActive: true);
        SeedLearningActivityProgress(db, student1, la, ProgressStatus.Completed, DateTime.UtcNow.AddDays(-1));

        SeedStudentBadge(db, student1, DateTime.UtcNow.AddDays(-1));

        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassDashboardAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        var dto = result.Data;
        Assert.Equal(cls.Id, dto.ClassId);
        Assert.Equal("3A", dto.ClassName);
        Assert.Equal(2, dto.StudentsCount);
        Assert.Equal(1, dto.ActiveTodayCount);
        Assert.Equal(1, dto.InactiveTodayCount);
        Assert.Equal(50, dto.TotalPagesReadLast7Days);
        Assert.Equal(1, dto.CompletedAssignmentsLast7Days);
        Assert.Equal(1, dto.ActiveLearningActivitiesCount);
        Assert.Equal(1, dto.CompletedLearningActivitiesLast7Days);
        Assert.NotEmpty(dto.Leaderboard);
        Assert.NotEmpty(dto.BestStreaks);
        Assert.NotEmpty(dto.RecentBadges);
        Assert.NotEmpty(dto.TopReaders);
    }

[Fact]
    public async Task GetClassStudentActivityAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetClassStudentActivityAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetClassStudentActivityAsync_WhenClassFound_ReturnsStudentActivity()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student1 = SeedStudent(db, cls.Id, "Alice", "Smith");
        var student2 = SeedStudent(db, cls.Id, "Bob", "Jones");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createdNow = DateTime.UtcNow;

        SeedLog(db, student1, ActivityType.ReadingProgress, today, pagesRead: 15, createdAt: createdNow);
        SeedLog(db, student1, ActivityType.AssignmentCompleted, today, createdAt: createdNow);
        SeedLog(db, student1, ActivityType.ReadingProgress, today.AddDays(-5), createdAt: createdNow.AddDays(-5));

        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetClassStudentActivityAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);

        var alice = result.Data[0];
        Assert.Equal("Alice Smith", alice.StudentName);
        Assert.Equal(15, alice.PagesReadToday);
        Assert.Equal(1, alice.AssignmentsCompletedToday);
        Assert.True(alice.IsActiveToday);
        Assert.NotNull(alice.LastActivityAt);

        var bob = result.Data[1];
        Assert.Equal("Bob Jones", bob.StudentName);
        Assert.Equal(0, bob.PagesReadToday);
        Assert.False(bob.IsActiveToday);
        Assert.Null(bob.LastActivityAt);
    }

[Fact]
    public async Task GetStudentDetailsAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentDetailsAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetStudentDetailsAsync_WhenClassNotBelongingToTeacher_ReturnsForbidden()
    {
        await using var db = CreateDbContext();
        var otherCls = new Class
        {
            OrganizationId = OrgId,
            TeacherId = Guid.NewGuid(),
            Name = "3A",
            Grade = 3,
            SchoolYear = "2024/2025"
        };
        db.Classes.Add(otherCls);
        var student = SeedStudent(db, otherCls.Id, "Alice", "Smith");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentDetailsAsync(student.Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Forbidden.", result.Error);
    }

    [Fact]
    public async Task GetStudentDetailsAsync_WhenSuccessWithNoActivityLogs_StatsAreZero()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, "Alice", "Smith");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentDetailsAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        var dto = result.Data;
        Assert.Equal("Alice Smith", dto.StudentName);
        Assert.Equal(0, dto.TotalPagesRead);
        Assert.Equal(0, dto.CompletedAssignments);
        Assert.Equal(0, dto.TotalPoints);
        Assert.Empty(dto.ActivityLast7Days);
        Assert.Empty(dto.Reading);
        Assert.Empty(dto.Assignments);
        Assert.Empty(dto.LearningActivities);
        Assert.Empty(dto.Challenges);
    }

    [Fact]
    public async Task GetStudentDetailsAsync_WhenSuccessWithAllActivityTypes_ReturnsCorrectData()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, "Alice", "Smith");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        SeedLog(db, student, ActivityType.ReadingProgress, today, pagesRead: 10, pointsEarned: 5);
        SeedLog(db, student, ActivityType.AssignmentCompleted, today, pointsEarned: 10);
        SeedLog(db, student, ActivityType.AssignmentStarted, today, pointsEarned: 0);
        SeedLog(db, student, ActivityType.ChallengeCompleted, today, pointsEarned: 20);
        SeedLog(db, student, ActivityType.ChallengeProgressUpdated, today, pointsEarned: 0);
        SeedLog(db, student, ActivityType.LearningActivityCompleted,today, pointsEarned: 15);
        SeedLog(db, student, ActivityType.LearningActivityStarted, today, pointsEarned: 0);

        var (_, ab1) = SeedAssignedBook(db, cls.Id, endDateUtc: null);
        var (_, ab2) = SeedAssignedBook(db, cls.Id, endDateUtc: DateTime.UtcNow.AddDays(-1));
        SeedReadingProgress(db, student, ab1);
        SeedReadingProgress(db, student, ab2);

        var asgn1 = SeedAssignment(db, cls.Id, dueDate: null);
        var asgn2 = SeedAssignment(db, cls.Id, dueDate: DateTime.UtcNow.AddDays(-1));
        SeedAssignmentProgress(db, student, asgn1);
        SeedAssignmentProgress(db, student, asgn2);

        var la = SeedLearningActivity(db, cls.Id, dueDate: DateTime.UtcNow.AddDays(5));
        SeedLearningActivityProgress(db, student, la, ProgressStatus.InProgress);

        var challenge = SeedChallenge(db, cls, endDate: DateTime.UtcNow.AddDays(-1));
        SeedChallengeProgress(db, student, challenge, completed: false, startedAt: DateTime.UtcNow);

        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentDetailsAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        var dto = result.Data;

        Assert.Equal(7, dto.ActivityLast7Days.Count);
        var descs = dto.ActivityLast7Days.Select(a => a.Description).ToList();
        Assert.Contains(descs, d => d.StartsWith("Read"));
        Assert.Contains(descs, d => d == "Completed assignment");
        Assert.Contains(descs, d => d == "Started assignment");
        Assert.Contains(descs, d => d == "Completed challenge");
        Assert.Contains(descs, d => d == "Updated challenge progress");
        Assert.Contains(descs, d => d == "Completed learning activity");
        Assert.Contains(descs, d => d == "Started learning activity");

        Assert.Equal(50, dto.TotalPoints);
        Assert.Equal(10, dto.TotalPagesRead);
        Assert.Equal(1, dto.CompletedAssignments);

        Assert.Equal(2, dto.Reading.Count);
        Assert.Contains(dto.Reading, r => r.IsExpired);
        Assert.Contains(dto.Reading, r => !r.IsExpired);

        Assert.Equal(2, dto.Assignments.Count);
        Assert.Contains(dto.Assignments, a => a.IsExpired);
        Assert.Contains(dto.Assignments, a => !a.IsExpired);

        Assert.Single(dto.LearningActivities);
        Assert.False(dto.LearningActivities[0].IsExpired);

        Assert.Single(dto.Challenges);
        Assert.True(dto.Challenges[0].IsExpired);
        Assert.True(dto.Challenges[0].Started);
    }

    [Fact]
    public async Task GetStudentDetailsAsync_WhenActivityTypeIsUnknown_UsesDefaultDescription()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, "Alice", "Smith");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        SeedLog(db, student, (ActivityType)99, today, pointsEarned: 0);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentDetailsAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data.ActivityLast7Days);
        Assert.Equal("Activity", result.Data.ActivityLast7Days[0].Description);
    }

[Fact]
    public async Task GetStudentBadgesAsync_WhenStudentNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetStudentBadgesAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task GetStudentBadgesAsync_WhenForbidden_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var otherCls = new Class
        {
            OrganizationId = OrgId,
            TeacherId = Guid.NewGuid(),
            Name = "3A",
            Grade = 3,
            SchoolYear = "2024/2025"
        };
        db.Classes.Add(otherCls);
        var student = SeedStudent(db, otherCls.Id, "Alice", "Smith");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentBadgesAsync(student.Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Forbidden.", result.Error);
    }

    [Fact]
    public async Task GetStudentBadgesAsync_WhenSuccess_ReturnsBadgesOrderedByAwardedAt()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        var student = SeedStudent(db, cls.Id, "Alice", "Smith");

        var badge1 = new Badge { Code = "BADGE_A", Name = "Badge A", Description = "Desc A", Icon = "a" };
        var badge2 = new Badge { Code = "BADGE_B", Name = "Badge B", Description = "Desc B", Icon = "b" };
        db.Badges.Add(badge1);
        db.Badges.Add(badge2);

        db.StudentBadges.Add(new StudentBadge
        {
            StudentProfileId = student.Id,
            BadgeId = badge1.Id,
            Badge = badge1,
            AwardedAt = DateTime.UtcNow.AddDays(-2)
        });
        db.StudentBadges.Add(new StudentBadge
        {
            StudentProfileId = student.Id,
            BadgeId = badge2.Id,
            Badge = badge2,
            AwardedAt = DateTime.UtcNow.AddDays(-1)
        });

        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetStudentBadgesAsync(student.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("BADGE_B", result.Data[0].Code);
        Assert.Equal("BADGE_A", result.Data[1].Code);
    }
}
