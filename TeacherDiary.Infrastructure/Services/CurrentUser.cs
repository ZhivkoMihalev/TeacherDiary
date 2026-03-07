using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TeacherDiary.Application.Abstractions.Services;

namespace TeacherDiary.Infrastructure.Services;

public sealed class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public Guid UserId =>
        Guid.TryParse(http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      http.HttpContext?.User?.FindFirstValue("sub"), out var id)
            ? id 
            : Guid.Empty;

    public Guid OrganizationId =>
        Guid.TryParse(http.HttpContext?.User?.FindFirstValue("organizationId"), out var id)
            ? id 
            : Guid.Empty;

    public bool IsInRole(string role) => http.HttpContext?.User?.IsInRole(role) ?? false;

    public bool IsAuthenticated => http.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
