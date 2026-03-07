namespace TeacherDiary.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } = default!;

    public DateTime ExpiresAt { get; set; }

    public string Email { get; set; } = default!;

    public string[] Roles { get; set; } = [];
}
