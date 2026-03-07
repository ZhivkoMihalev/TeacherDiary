namespace TeacherDiary.Application.DTOs.Auth;

public sealed class RegisterRequest
{
    public string Email { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string OrganizationName { get; set; }
}