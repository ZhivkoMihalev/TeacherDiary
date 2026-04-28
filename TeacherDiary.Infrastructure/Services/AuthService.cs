using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherDiary.Application.Abstractions.Auth;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Auth;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class AuthService(
    AppDbContext db,
    UserManager<AppUser> users,
    RoleManager<AppRole> roles,
    IOptions<JwtOptions> jwt) : IAuthService
{
    private readonly JwtOptions _jwt = jwt.Value;

    public const string RoleTeacher = "Teacher";
    public const string RoleParent = "Parent";
    public const string RoleAdmin = "Admin";

    public async Task<Result<AuthResponse>> RegisterTeacherAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        await EnsureRolesAsync(cancellationToken);

        if (await users.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponse>.Fail($"Email: {request.Email} already exists.");

        var org = new Organization
        {
            Name = request.OrganizationName,
            Type = OrganizationType.Teacher,
            SubscriptionPlan = "Trial",
            SubscriptionStart = DateTime.UtcNow,
            SubscriptionEnd = DateTime.UtcNow.AddDays(30)
        };

        db.Organizations.Add(org);
        await db.SaveChangesAsync(cancellationToken);

        var user = new AppUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            OrganizationId = org.Id
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded)
            return Result<AuthResponse>.Fail(string.Join("; ", created.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, RoleTeacher);

        var roles = await users.GetRolesAsync(user);
        var (token, _) = JwtTokenGenerator.CreateToken(user, roles, _jwt);

        return Result<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id.ToString(),
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    public async Task<Result<AuthResponse>> RegisterParentAsync(
        RegisterParentRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureRolesAsync(cancellationToken);

        if (await users.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponse>.Fail($"Email: {request.Email} already exists.");

        var parent = new AppUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            OrganizationId = null // Parent още не принадлежи към организация
        };

        var created = await users.CreateAsync(parent, request.Password);

        if (!created.Succeeded)
            return Result<AuthResponse>.Fail(string.Join("; ", created.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(parent, RoleParent);

        var roles = await users.GetRolesAsync(parent);

        var (token, _) = JwtTokenGenerator.CreateToken(parent, roles, _jwt);

        return Result<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            UserId = parent.Id.ToString(),
            Email = parent.Email!,
            FullName = $"{parent.FirstName} {parent.LastName}",
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        if (user is null) return Result<AuthResponse>.Fail("Invalid credentials.");

        var successLogin = await users.CheckPasswordAsync(user, request.Password);
        if (!successLogin) return Result<AuthResponse>.Fail("Invalid credentials.");

        var roles = await users.GetRolesAsync(user);
        var (token, _) = JwtTokenGenerator.CreateToken(user, roles, _jwt);

        return Result<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            UserId = user.Id.ToString(),
            Email = user.Email!,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    private async Task EnsureRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var role in new[] { RoleTeacher, RoleParent, RoleAdmin })
        {
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new AppRole { Name = role });
        }
    }
}
