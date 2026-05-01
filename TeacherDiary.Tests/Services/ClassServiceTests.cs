using Microsoft.EntityFrameworkCore;
using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Classes;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class ClassServiceTests
{
    private static readonly Guid TeacherId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OrgId = new("22222222-2222-2222-2222-222222222222");

    private readonly Mock<ICurrentUser> _currentUserMock = new();

    public ClassServiceTests()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(TeacherId);
        _currentUserMock.Setup(x => x.OrganizationId).Returns(OrgId);
    }

    private AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private ClassService CreateService(AppDbContext db) =>
        new(db, _currentUserMock.Object);

    // -----------------------------------------------------------------------
    // Seed helpers
    // -----------------------------------------------------------------------

    private static Class SeedClass(AppDbContext db, string name = "3A")
    {
        var cls = new Class
        {
            OrganizationId = OrgId,
            TeacherId = TeacherId,
            Name = name,
            Grade = 3,
            SchoolYear = "2024/2025"
        };
        db.Classes.Add(cls);
        return cls;
    }

    private static void SeedStudent(AppDbContext db, Guid classId)
    {
        db.Students.Add(new StudentProfile
        {
            ClassId = classId,
            FirstName = "Alice",
            LastName = "Smith"
        });
    }

    // -----------------------------------------------------------------------
    // CreateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenUserIdIsEmpty_ReturnsFail()
    {
        _currentUserMock.Setup(x => x.UserId).Returns(Guid.Empty);
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateAsync(
            new ClassCreateRequest { Name = "3A", Grade = 3, SchoolYear = "2024/2025" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Unauthorized.", result.Error);
    }

    [Fact]
    public async Task CreateAsync_WhenUserIdIsValid_ReturnsOkAndPersistsClass()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.CreateAsync(
            new ClassCreateRequest { Name = "5B", Grade = 5, SchoolYear = "2024/2025" },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("5B", result.Data.Name);
        Assert.Equal(5, result.Data.Grade);
        Assert.Equal("2024/2025", result.Data.SchoolYear);
        Assert.Equal(0, result.Data.StudentsCount);
        Assert.NotEqual(Guid.Empty, result.Data.Id);
        Assert.Equal(1, db.Classes.Count());
    }

    // -----------------------------------------------------------------------
    // GetMyClassesAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetMyClassesAsync_WhenNoClasses_ReturnsEmptyList()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.GetMyClassesAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetMyClassesAsync_WhenClassesExist_ReturnsClassDtosWithStudentCount()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db, "4C");
        await db.SaveChangesAsync();
        SeedStudent(db, cls.Id);
        SeedStudent(db, cls.Id);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyClassesAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Single(result.Data);
        var dto = result.Data[0];
        Assert.Equal("4C", dto.Name);
        Assert.Equal(3, dto.Grade);
        Assert.Equal(2, dto.StudentsCount);
    }

    [Fact]
    public async Task GetMyClassesAsync_DoesNotReturnClassesBelongingToOtherTeachers()
    {
        await using var db = CreateDbContext();
        db.Classes.Add(new Class
        {
            OrganizationId = OrgId,
            TeacherId = Guid.NewGuid(),
            Name = "OtherTeacher",
            Grade = 1,
            SchoolYear = "2024/2025"
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetMyClassesAsync(CancellationToken.None);

        Assert.True(result.Success);
        Assert.Empty(result.Data);
    }

    // -----------------------------------------------------------------------
    // UpdateAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.UpdateAsync(
            Guid.NewGuid(),
            new ClassUpdateRequest { Name = "New", Grade = 4, SchoolYear = "2025/2026" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenClassFound_UpdatesFieldsAndReturnsOk()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db, "3A");
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.UpdateAsync(
            cls.Id,
            new ClassUpdateRequest { Name = "4B", Grade = 4, SchoolYear = "2025/2026" },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
        var updated = db.Classes.Find(cls.Id)!;
        Assert.Equal("4B", updated.Name);
        Assert.Equal(4, updated.Grade);
        Assert.Equal("2025/2026", updated.SchoolYear);
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenClassNotFound_ReturnsFail()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var result = await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("was not found", result.Error);
    }

    [Fact]
    public async Task DeleteAsync_WhenClassFound_RemovesClassAndReturnsOk()
    {
        await using var db = CreateDbContext();
        var cls = SeedClass(db);
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.DeleteAsync(cls.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Equal(0, db.Classes.Count());
    }
}
