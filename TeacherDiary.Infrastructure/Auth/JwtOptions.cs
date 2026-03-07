namespace TeacherDiary.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public string SigningKey { get; set; } = default!;

    public int ExpirationMinutes { get; set; } = 120;
}
