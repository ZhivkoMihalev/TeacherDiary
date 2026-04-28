namespace TeacherDiary.Application.DTOs.Auth;

public sealed class AuthResponse
{
    public string Token { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Role { get; set; } = default!;
}
