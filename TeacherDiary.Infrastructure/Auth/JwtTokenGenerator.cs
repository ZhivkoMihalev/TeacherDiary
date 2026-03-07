using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TeacherDiary.Infrastructure.Auth;

public static class JwtTokenGenerator
{
    public static (string token, DateTime expiresAt) CreateToken(
        AppUser user,
        IEnumerable<string> roles,
        JwtOptions options)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("organizationId", user.OrganizationId.ToString())
        };

        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

        var expires = DateTime.UtcNow.AddMinutes(options.ExpirationMinutes);

        var jwt = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
