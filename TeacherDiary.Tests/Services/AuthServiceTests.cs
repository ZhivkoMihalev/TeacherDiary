using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TeacherDiary.Application.DTOs.Auth;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class AuthServiceTests
{
    // -----------------------------------------------------------------------
    // Fixture — wires up real Identity with an InMemory EF Core database
    // -----------------------------------------------------------------------

    private sealed class AuthServiceFixture : IDisposable
    {
        public AppDbContext Db { get; }
        public UserManager<AppUser> Users { get; }
        public RoleManager<AppRole> Roles { get; }
        public AuthService Service { get; }

        public AuthServiceFixture(Action<PasswordOptions>? configurePassword = null)
        {
            var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            Db = new AppDbContext(dbOptions);

            var identityOpts = new IdentityOptions();
            identityOpts.Password.RequireDigit = false;
            identityOpts.Password.RequireLowercase = false;
            identityOpts.Password.RequireUppercase = false;
            identityOpts.Password.RequireNonAlphanumeric = false;
            identityOpts.Password.RequiredLength = 4;
            configurePassword?.Invoke(identityOpts.Password);

            var userStore = new UserStore<AppUser, AppRole, AppDbContext, Guid>(Db);
            var roleStore = new RoleStore<AppRole, AppDbContext, Guid>(Db);
            var normalizer = new UpperInvariantLookupNormalizer();
            var errorDescriber = new IdentityErrorDescriber();

            Roles = new RoleManager<AppRole>(
                roleStore,
                Enumerable.Empty<IRoleValidator<AppRole>>(),
                normalizer,
                errorDescriber,
                NullLogger<RoleManager<AppRole>>.Instance);

            Users = new UserManager<AppUser>(
                userStore,
                Options.Create(identityOpts),
                new PasswordHasher<AppUser>(),
                new IUserValidator<AppUser>[] { new UserValidator<AppUser>() },
                new IPasswordValidator<AppUser>[] { new PasswordValidator<AppUser>() },
                normalizer,
                errorDescriber,
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<UserManager<AppUser>>.Instance);

            var jwtOptions = Options.Create(new JwtOptions
            {
                Issuer = "test",
                Audience = "test",
                SigningKey = new string('x', 64),
                ExpirationMinutes = 60
            });

            Service = new AuthService(Db, Users, Roles, jwtOptions);
        }

        public void Dispose()
        {
            Users.Dispose();
            Roles.Dispose();
            Db.Dispose();
        }
    }

    // -----------------------------------------------------------------------
    // Request helpers
    // -----------------------------------------------------------------------

    private static RegisterRequest TeacherRequest(string email = "teacher@test.com") => new()
    {
        Email = email,
        Password = "Pass1234",
        FirstName = "John",
        LastName = "Doe",
        OrganizationName = "Test School"
    };

    private static RegisterParentRequest ParentRequest(string email = "parent@test.com") => new()
    {
        Email = email,
        Password = "Pass1234",
        FirstName = "Jane",
        LastName = "Smith"
    };

    private static RegisterStudentRequest StudentRequest(string email = "student@test.com") => new()
    {
        Email = email,
        Password = "Pass1234",
        FirstName = "Alice",
        LastName = "Brown"
    };

    // -----------------------------------------------------------------------
    // RegisterTeacherAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RegisterTeacherAsync_WhenEmailAlreadyExists_ReturnsFail()
    {
        using var f = new AuthServiceFixture();
        await f.Users.CreateAsync(
            new AppUser { Email = "teacher@test.com", UserName = "teacher@test.com", FirstName = "X", LastName = "X" },
            "Pass1234");

        var result = await f.Service.RegisterTeacherAsync(TeacherRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("teacher@test.com", result.Error);
    }

    [Fact]
    public async Task RegisterTeacherAsync_WhenUserCreationFails_ReturnsFail()
    {
        using var f = new AuthServiceFixture(p =>
        {
            p.RequireDigit = true;
            p.RequireUppercase = true;
            p.RequiredLength = 12;
        });

        var request = new RegisterRequest
        {
            Email = "teacher@test.com",
            Password = "weak",
            FirstName = "John",
            LastName = "Doe",
            OrganizationName = "Test School"
        };

        var result = await f.Service.RegisterTeacherAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task RegisterTeacherAsync_WhenSuccess_ReturnsOkWithCorrectData()
    {
        using var f = new AuthServiceFixture();

        var result = await f.Service.RegisterTeacherAsync(TeacherRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data.Token);
        Assert.Equal("teacher@test.com", result.Data.Email);
        Assert.Equal("John Doe", result.Data.FullName);
        Assert.Equal(AuthService.RoleTeacher, result.Data.Role);
        Assert.NotEmpty(result.Data.UserId);
    }

    [Fact]
    public async Task RegisterTeacherAsync_WhenSuccess_PersistsOrganizationToDatabase()
    {
        using var f = new AuthServiceFixture();

        await f.Service.RegisterTeacherAsync(TeacherRequest(), CancellationToken.None);

        var org = f.Db.Organizations.Single();
        Assert.Equal("Test School", org.Name);
    }

    [Fact]
    public async Task RegisterTeacherAsync_WhenRolesAlreadyExist_StillSucceeds()
    {
        using var f = new AuthServiceFixture();

        // First call creates all four roles
        await f.Service.RegisterTeacherAsync(TeacherRequest("first@test.com"), CancellationToken.None);

        // Second call: EnsureRolesAsync finds existing roles and skips creation
        var result = await f.Service.RegisterTeacherAsync(TeacherRequest("second@test.com"), CancellationToken.None);

        Assert.True(result.Success);
    }

    // -----------------------------------------------------------------------
    // RegisterParentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RegisterParentAsync_WhenEmailAlreadyExists_ReturnsFail()
    {
        using var f = new AuthServiceFixture();
        await f.Users.CreateAsync(
            new AppUser { Email = "parent@test.com", UserName = "parent@test.com", FirstName = "X", LastName = "X" },
            "Pass1234");

        var result = await f.Service.RegisterParentAsync(ParentRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("parent@test.com", result.Error);
    }

    [Fact]
    public async Task RegisterParentAsync_WhenUserCreationFails_ReturnsFail()
    {
        using var f = new AuthServiceFixture(p =>
        {
            p.RequireDigit = true;
            p.RequireUppercase = true;
            p.RequiredLength = 12;
        });

        var request = new RegisterParentRequest
        {
            Email = "parent@test.com",
            Password = "weak",
            FirstName = "Jane",
            LastName = "Smith"
        };

        var result = await f.Service.RegisterParentAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task RegisterParentAsync_WhenSuccess_ReturnsOkWithCorrectData()
    {
        using var f = new AuthServiceFixture();

        var result = await f.Service.RegisterParentAsync(ParentRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data.Token);
        Assert.Equal("parent@test.com", result.Data.Email);
        Assert.Equal("Jane Smith", result.Data.FullName);
        Assert.Equal(AuthService.RoleParent, result.Data.Role);
        Assert.NotEmpty(result.Data.UserId);
    }

    // -----------------------------------------------------------------------
    // RegisterStudentAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RegisterStudentAsync_WhenEmailAlreadyExists_ReturnsFail()
    {
        using var f = new AuthServiceFixture();
        await f.Users.CreateAsync(
            new AppUser { Email = "student@test.com", UserName = "student@test.com", FirstName = "X", LastName = "X" },
            "Pass1234");

        var result = await f.Service.RegisterStudentAsync(StudentRequest(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("student@test.com", result.Error);
    }

    [Fact]
    public async Task RegisterStudentAsync_WhenUserCreationFails_ReturnsFail()
    {
        using var f = new AuthServiceFixture(p =>
        {
            p.RequireDigit = true;
            p.RequireUppercase = true;
            p.RequiredLength = 12;
        });

        var request = new RegisterStudentRequest
        {
            Email = "student@test.com",
            Password = "weak",
            FirstName = "Alice",
            LastName = "Brown"
        };

        var result = await f.Service.RegisterStudentAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public async Task RegisterStudentAsync_WhenSuccess_ReturnsOkWithCorrectData()
    {
        using var f = new AuthServiceFixture();

        var result = await f.Service.RegisterStudentAsync(StudentRequest(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data.Token);
        Assert.Equal("student@test.com", result.Data.Email);
        Assert.Equal("Alice Brown", result.Data.FullName);
        Assert.Equal(AuthService.RoleStudent, result.Data.Role);
        Assert.NotEmpty(result.Data.UserId);
    }

    [Fact]
    public async Task RegisterStudentAsync_WhenSuccess_PersistsStudentProfileToDatabase()
    {
        using var f = new AuthServiceFixture();

        var result = await f.Service.RegisterStudentAsync(StudentRequest(), CancellationToken.None);

        var profile = f.Db.Students.Single();
        Assert.Equal("Alice", profile.FirstName);
        Assert.Equal("Brown", profile.LastName);
        Assert.Equal(Guid.Parse(result.Data.UserId), profile.UserId);
    }

    // -----------------------------------------------------------------------
    // LoginAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFail()
    {
        using var f = new AuthServiceFixture();

        var result = await f.Service.LoginAsync(
            new LoginRequest { Email = "nobody@test.com", Password = "Pass1234" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid credentials.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WhenWrongPassword_ReturnsFail()
    {
        using var f = new AuthServiceFixture();
        await f.Service.RegisterTeacherAsync(TeacherRequest(), CancellationToken.None);

        var result = await f.Service.LoginAsync(
            new LoginRequest { Email = "teacher@test.com", Password = "WrongPass" },
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Invalid credentials.", result.Error);
    }

    [Fact]
    public async Task LoginAsync_WhenSuccess_ReturnsOkWithCorrectData()
    {
        using var f = new AuthServiceFixture();
        await f.Service.RegisterTeacherAsync(TeacherRequest(), CancellationToken.None);

        var result = await f.Service.LoginAsync(
            new LoginRequest { Email = "teacher@test.com", Password = "Pass1234" },
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotEmpty(result.Data.Token);
        Assert.Equal("teacher@test.com", result.Data.Email);
        Assert.Equal("John Doe", result.Data.FullName);
        Assert.Equal(AuthService.RoleTeacher, result.Data.Role);
        Assert.NotEmpty(result.Data.UserId);
    }
}
