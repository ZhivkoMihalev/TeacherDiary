using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Auth;
using TeacherDiary.Application.DTOs.Auth;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Tags("Authentication")]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>
    /// Registers a new teacher account.
    /// </summary>
    /// <remarks>
    /// Creates a teacher account in the system and returns authentication data.
    /// </remarks>
    /// <param name="request">Teacher registration information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result containing tokens or user data.</returns>
    [HttpPost("register-teacher")]
    public async Task<IActionResult> RegisterTeacher([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterTeacherAsync(request, cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Authenticates a user and returns authentication tokens.
    /// </summary>
    /// <remarks>
    /// Used by teachers or parents to log into the system.
    /// </remarks>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication token and user information.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : Unauthorized(new { error = result.Error });
    }
}
