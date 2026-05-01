using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Notifications;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class NotificationServiceTests
{
    private static readonly Guid MyId    = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser>       _currentUserMock = new();
    private readonly Mock<INotificationPusher> _pusherMock      = new();

    public NotificationServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(MyId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // SQLite in-memory is required for ExecuteUpdateAsync (not supported by InMemory provider).
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

    private NotificationService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object, _pusherMock.Object);

    private static Notification SeedNotification(
        AppDbContext db,
        Guid userId,
        bool isRead = false,
        bool isDeleted = false,
        string message = "Test",
        DateTime? createdAt = null)
    {
        var n = new Notification
        {
            UserId = userId,
            Type = NotificationType.AssignmentCreated,
            Message = message,
            IsRead = isRead,
            IsDeleted = isDeleted,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        db.Notifications.Add(n);
        return n;
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenCalled_PersistsNotificationAndCallsPusher()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);
        var refId = Guid.NewGuid();

        await service.CreateAsync(
            OtherId,
            NotificationType.BadgeEarned,
            "You earned a badge!",
            "/badges",
            refId,
            CancellationToken.None);

        var saved = db.Notifications.Local.Single();
        Assert.Equal(OtherId, saved.UserId);
        Assert.Equal(NotificationType.BadgeEarned, saved.Type);
        Assert.Equal("You earned a badge!", saved.Message);
        Assert.Equal("/badges", saved.NavigationUrl);
        Assert.Equal(refId, saved.ReferenceId);
        Assert.False(saved.IsRead);

        _pusherMock.Verify(
            p => p.PushAsync(
                OtherId,
                It.Is<NotificationDto>(d =>
                    d.Message == "You earned a badge!" &&
                    d.Type == NotificationType.BadgeEarned &&
                    !d.IsRead &&
                    d.NavigationUrl == "/badges"),
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNavigationUrlAndReferenceIdAreNull_PersistsWithNulls()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.CreateAsync(
            OtherId,
            NotificationType.StreakReminder,
            "Keep your streak!",
            null,
            null,
            CancellationToken.None);

        var saved = db.Notifications.Local.Single();
        Assert.Null(saved.NavigationUrl);
        Assert.Null(saved.ReferenceId);
    }

    // -----------------------------------------------------------------------
    // GetForUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetForUserAsync_WhenCalled_ReturnsOnlyNonDeletedForCurrentUser()
    {
        await using var db = CreateDbContext();
        SeedNotification(db, MyId, message: "Mine");             // included
        SeedNotification(db, MyId, isDeleted: true);             // deleted → excluded (!n.IsDeleted false branch)
        SeedNotification(db, OtherId, message: "NotMine");       // other user → excluded (UserId != branch)
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetForUserAsync(1, 10, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Mine", result[0].Message);
    }

    [Fact]
    public async Task GetForUserAsync_WhenCalled_ReturnsOrderedByCreatedAtDescending()
    {
        await using var db = CreateDbContext();
        SeedNotification(db, MyId, message: "Old", createdAt: DateTime.UtcNow.AddMinutes(-10));
        SeedNotification(db, MyId, message: "New", createdAt: DateTime.UtcNow);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetForUserAsync(1, 10, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal("New", result[0].Message);
        Assert.Equal("Old", result[1].Message);
    }

    [Fact]
    public async Task GetForUserAsync_WhenPageIsTwo_SkipsFirstPageItems()
    {
        await using var db = CreateDbContext();
        for (var i = 0; i < 3; i++)
            SeedNotification(db, MyId, message: $"N{i}", createdAt: DateTime.UtcNow.AddMinutes(-i));
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetForUserAsync(page: 2, pageSize: 2, CancellationToken.None);

        Assert.Single(result);   // 3 total, skip 2 on page 1 → 1 remains on page 2
    }

    // -----------------------------------------------------------------------
    // GetUnreadCountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUnreadCountAsync_WhenCalled_CountsOnlyUnreadNonDeletedForCurrentUser()
    {
        await using var db = CreateDbContext();
        SeedNotification(db, MyId, isRead: false);                     // counted
        SeedNotification(db, MyId, isRead: false);                     // counted
        SeedNotification(db, MyId, isRead: true);                      // read → excluded (!n.IsRead false branch)
        SeedNotification(db, MyId, isRead: false, isDeleted: true);    // deleted → excluded (!n.IsDeleted false branch)
        SeedNotification(db, OtherId, isRead: false);                  // other user → excluded (UserId != branch)
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var count = await service.GetUnreadCountAsync(CancellationToken.None);

        Assert.Equal(2, count);
    }

    // -----------------------------------------------------------------------
    // MarkAsReadAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationNotFound_ReturnsEarly()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        // Should not throw; notification is null branch → early return
        await service.MarkAsReadAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Empty(db.Notifications.Local);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationBelongsToOtherUser_ReturnsEarlyWithoutUpdate()
    {
        await using var db = CreateDbContext();
        var n = SeedNotification(db, OtherId, isRead: false);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.MarkAsReadAsync(n.Id, CancellationToken.None);

        Assert.False(db.Notifications.Local.Single().IsRead);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationFoundForCurrentUser_SetsIsReadTrue()
    {
        await using var db = CreateDbContext();
        var n = SeedNotification(db, MyId, isRead: false);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await service.MarkAsReadAsync(n.Id, CancellationToken.None);

        Assert.True(db.Notifications.Local.Single().IsRead);
    }

    // -----------------------------------------------------------------------
    // MarkAllAsReadAsync  (SQLite required — ExecuteUpdateAsync)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task MarkAllAsReadAsync_WhenCalled_MarksAllUnreadNotificationsAsRead()
    {
        var (db, conn) = CreateSqliteDbContext();
        await using (db)
        await using (conn)
        {
            SeedNotification(db, MyId, isRead: false);
            SeedNotification(db, MyId, isRead: false);
            SeedNotification(db, MyId, isRead: true);   // already read — remains read
            await db.SaveChangesAsync();
            var service = CreateService(db);

            await service.MarkAllAsReadAsync(CancellationToken.None);

            var stillUnread = await db.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == MyId && !n.IsRead);
            Assert.Equal(0, stillUnread);
        }
    }
}
