namespace TeacherDiary.Application.Abstractions.Services;

public interface ICurrentUser
{
    Guid UserId { get; }

    Guid OrganizationId { get; }

    bool IsInRole(string role);

    bool IsAuthenticated { get; }
}
