using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Messages;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class MessageServiceTests
{
    private static readonly Guid MyId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public MessageServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(MyId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    // SQLite in-memory supports ExecuteUpdateAsync; InMemory provider does not.
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

    private MessageService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static AppUser SeedUser(AppDbContext db, Guid id, string firstName, string lastName)
    {
        var user = new AppUser
        {
            Id = id,
            UserName = $"{id}@test.com",
            FirstName = firstName,
            LastName = lastName
        };
        db.Users.Add(user);
        return user;
    }

    private static Message SeedMessage(
        AppDbContext db,
        Guid senderId, Guid receiverId,
        string? content = "Hello",
        string? imageUrl = null,
        bool isRead = false,
        DateTime? createdAt = null)
    {
        var msg = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            ImageUrl = imageUrl,
            IsRead = isRead,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        db.Messages.Add(msg);
        return msg;
    }

    private static Class SeedClass(AppDbContext db, Guid teacherId)
    {
        var cls = new Class
        {
            OrganizationId = Guid.NewGuid(),
            TeacherId = teacherId,
            Name = "3A",
            Grade = 3,
            SchoolYear = "2024/2025"
        };
        db.Classes.Add(cls);
        return cls;
    }

    private static StudentProfile SeedStudent(
        AppDbContext db, Guid classId,
        Guid? parentId = null, Guid? userId = null)
    {
        var s = new StudentProfile
        {
            ClassId = classId,
            FirstName = "Alice",
            LastName = "Smith",
            ParentId = parentId,
            UserId = userId
        };
        db.Students.Add(s);
        return s;
    }

    // -----------------------------------------------------------------------
    // GetConversationsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetConversationsAsync_WhenTeacherHasClassWithParentedStudent_IncludesStudentName()
    {
        await using var db = CreateDbContext();
        SeedUser(db, OtherId, "Jane", "Parent");
        var cls = SeedClass(db, MyId);
        SeedStudent(db, cls.Id, parentId: OtherId);   // has parent → studentNameMap entry
        SeedStudent(db, cls.Id, parentId: null);       // no parent → skipped in studentNameMap
        SeedMessage(db, MyId, OtherId, content: "Hi Jane");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetConversationsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        var conv = result.Data[0];
        Assert.Equal("Jane Parent", conv.OtherUserName);
        Assert.NotNull(conv.StudentName);          // studentNameMap hit (GetValueOrDefault found)
        Assert.Equal("Hi Jane", conv.LastMessage); // Content != null → last.Content ?? "" non-null branch
        Assert.False(conv.LastMessageIsImage);
        Assert.True(conv.LastMessageIsFromMe);
    }

    [Fact]
    public async Task GetConversationsAsync_WhenUserHasNoClasses_NoStudentNameMapBuilt()
    {
        await using var db = CreateDbContext();
        SeedUser(db, OtherId, "Bob", "Sender");
        SeedMessage(db, OtherId, MyId, content: "Hey", isRead: false);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetConversationsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        var conv = result.Data[0];
        Assert.Equal("Bob Sender", conv.OtherUserName);
        Assert.Null(conv.StudentName);             // studentNameMap miss (GetValueOrDefault null)
        Assert.Equal(1, conv.UnreadCount);
        Assert.False(conv.LastMessageIsFromMe);
    }

    [Fact]
    public async Task GetConversationsAsync_WhenLastMessageIsImageOnly_ShowsImagePlaceholder()
    {
        await using var db = CreateDbContext();
        SeedUser(db, OtherId, "Bob", "Jones");
        SeedMessage(db, OtherId, MyId, content: null, imageUrl: "http://img.png");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetConversationsAsync(CancellationToken.None);

        var conv = result.Data[0];
        Assert.Equal("[Снимка]", conv.LastMessage);
        Assert.True(conv.LastMessageIsImage);
    }

    [Fact]
    public async Task GetConversationsAsync_WhenLastMessageHasNeitherContentNorImage_LastMessageIsEmpty()
    {
        await using var db = CreateDbContext();
        // Unknown sender (not in db.Users) → "Непознат"; Content=null, ImageUrl=null → "" via ??
        var unknownId = Guid.NewGuid();
        SeedMessage(db, unknownId, MyId, content: null, imageUrl: null);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetConversationsAsync(CancellationToken.None);

        var conv = result.Data[0];
        Assert.Equal("Непознат", conv.OtherUserName);  // userMap miss → default fallback
        Assert.Equal("", conv.LastMessage);             // null ?? "" → null branch of ??
        Assert.False(conv.LastMessageIsImage);
    }

    // -----------------------------------------------------------------------
    // GetConversationAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetConversationAsync_WhenCalled_MarksUnreadInboundAsReadAndReturnsBothDirections()
    {
        // SQLite in-memory is required here because ExecuteUpdateAsync is not supported by the InMemory provider.
        var (db, conn) = CreateSqliteDbContext();
        await using (db)
        await using (conn)
        {
            var older = DateTime.UtcNow.AddMinutes(-5);
            var newer = DateTime.UtcNow;
            SeedMessage(db, MyId, OtherId, content: "Hi",  isRead: false, createdAt: older);
            SeedMessage(db, OtherId, MyId, content: "Hey", isRead: false, createdAt: newer);
            await db.SaveChangesAsync();
            var service = CreateService(db);

            var result = await service.GetConversationAsync(OtherId, CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal(2, result.Data.Count);
            // Ordered by CreatedAt ascending
            Assert.True(result.Data[0].IsFromMe);   // outbound
            Assert.False(result.Data[1].IsFromMe);  // inbound

            // ExecuteUpdateAsync marked inbound as read — verify via AsNoTracking to bypass stale change tracker
            var unread = await db.Messages
                .AsNoTracking()
                .CountAsync(m => m.SenderId == OtherId && m.ReceiverId == MyId && !m.IsRead);
            Assert.Equal(0, unread);
        }
    }

    // -----------------------------------------------------------------------
    // SendMessageAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SendMessageAsync_WhenBothContentAndImageEmpty_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.SendMessageAsync(
            new SendMessageRequest { ReceiverId = OtherId, Content = null, ImageUrl = null },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task SendMessageAsync_WhenContentProvided_CreatesTrimmedMessage()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.SendMessageAsync(
            new SendMessageRequest { ReceiverId = OtherId, Content = "  Hello  ", ImageUrl = null },
            CancellationToken.None);

        Assert.True(result.Success);
        var msg = db.Messages.Local.Single();
        Assert.Equal("Hello", msg.Content);   // trimmed
        Assert.Null(msg.ImageUrl);
        Assert.Equal(MyId, msg.SenderId);
        Assert.Equal(OtherId, msg.ReceiverId);
        Assert.False(msg.IsRead);
    }

    [Fact]
    public async Task SendMessageAsync_WhenOnlyImageProvided_CreatesMessageWithNullContent()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.SendMessageAsync(
            new SendMessageRequest { ReceiverId = OtherId, Content = null, ImageUrl = "http://img.png" },
            CancellationToken.None);

        Assert.True(result.Success);
        var msg = db.Messages.Local.Single();
        Assert.Null(msg.Content);              // IsNullOrWhiteSpace(null) → null stored
        Assert.Equal("http://img.png", msg.ImageUrl);
    }

    // -----------------------------------------------------------------------
    // GetUnreadCountAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUnreadCountAsync_WhenCalled_ReturnsUnreadInboundCount()
    {
        await using var db = CreateDbContext();
        SeedMessage(db, OtherId, MyId, content: "A", isRead: false);
        SeedMessage(db, OtherId, MyId, content: "B", isRead: false);
        SeedMessage(db, OtherId, MyId, content: "C", isRead: true);   // already read
        SeedMessage(db, MyId, OtherId, content: "D", isRead: false);  // I sent it, not counted
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetUnreadCountAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data);
    }

    // -----------------------------------------------------------------------
    // GetContactsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetContactsAsync_WhenTeacherMode_ReturnsParentAndSelfStudentContacts()
    {
        await using var db = CreateDbContext();
        var parentId = Guid.NewGuid();
        var selfUserId = Guid.NewGuid();
        SeedUser(db, parentId, "Jane", "Parent");
        SeedUser(db, selfUserId, "Bob", "Student");
        var cls = SeedClass(db, MyId);
        SeedStudent(db, cls.Id, parentId: parentId);        // has parent account
        SeedStudent(db, cls.Id, userId: selfUserId);        // self-registered, no parent
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetContactsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Contains(result.Data, c => c.FullName == "Jane Parent");
        Assert.Contains(result.Data, c => c.FullName == "Bob Student");
    }

    [Fact]
    public async Task GetContactsAsync_WhenStudentMode_TeacherFound_ReturnsTeacherContact()
    {
        await using var db = CreateDbContext();
        var teacherId = Guid.NewGuid();
        SeedUser(db, teacherId, "Mr", "Teacher");
        var cls = SeedClass(db, teacherId);    // class belongs to another teacher, not MyId
        db.Students.Add(new StudentProfile
        {
            ClassId = cls.Id,
            UserId = MyId,
            FirstName = "Alice",
            LastName = "Smith"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetContactsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal("Mr Teacher", result.Data[0].FullName);
        Assert.Equal(teacherId, result.Data[0].UserId);
    }

    [Fact]
    public async Task GetContactsAsync_WhenStudentMode_ClassNotFound_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        // Student points to a ClassId that does not exist in db
        var orphanClassId = Guid.NewGuid();
        db.Students.Add(new StudentProfile
        {
            ClassId = orphanClassId,
            UserId = MyId,
            FirstName = "Alice",
            LastName = "Smith"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetContactsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetContactsAsync_WhenStudentMode_TeacherUserNotFound_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var teacherId = Guid.NewGuid();             // class exists but teacher NOT in db.Users
        var cls = SeedClass(db, teacherId);
        db.Students.Add(new StudentProfile
        {
            ClassId = cls.Id,
            UserId = MyId,
            FirstName = "Alice",
            LastName = "Smith"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetContactsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetContactsAsync_WhenParentMode_ReturnsTeacherContacts()
    {
        await using var db = CreateDbContext();
        var teacherId = Guid.NewGuid();
        SeedUser(db, teacherId, "Mr", "Teacher");
        var cls = SeedClass(db, teacherId);
        // Student whose parent is MyId — no student profile with UserId == MyId exists
        SeedStudent(db, cls.Id, parentId: MyId);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetContactsAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        Assert.Equal("Mr Teacher", result.Data[0].FullName);
    }
}
