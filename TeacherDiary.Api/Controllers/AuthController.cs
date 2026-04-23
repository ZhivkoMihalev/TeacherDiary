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
    /// Creates a teacher user and an associated organization.
    /// Returns a JWT token that must be included in subsequent requests as a Bearer token.
    /// The JWT claims include the role "Teacher", the organization ID, and the user ID.
    /// </remarks>
    /// <param name="request">Teacher registration data (name, email, password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT token and basic user information.</returns>
    /// <response code="200">Registration successful — returns token and user data.</response>
    /// <response code="400">Email already taken or validation error.</response>
    [HttpPost("register-teacher")]
    public async Task<IActionResult> RegisterTeacher([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterTeacherAsync(request, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Registers a new parent account.
    /// </summary>
    /// <remarks>
    /// Creates a parent user. A parent can then create student profiles via <c>POST /api/parent/students</c>
    /// and link them to a class via the teacher's student enrollment flow.
    /// Returns a JWT token with role "Parent".
    /// </remarks>
    /// <param name="request">Parent registration data (name, email, password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT token and basic user information.</returns>
    /// <response code="200">Registration successful — returns token and user data.</response>
    /// <response code="400">Email already taken or validation error.</response>
    [HttpPost("register-parent")]
    public async Task<IActionResult> RegisterParent([FromBody] RegisterParentRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterParentAsync(request, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <remarks>
    /// Works for both teachers and parents. The returned token encodes the user's role,
    /// user ID, and — for teachers — their organization ID. Include the token in the
    /// <c>Authorization: Bearer &lt;token&gt;</c> header on all protected endpoints.
    /// </remarks>
    /// <param name="request">Login credentials (email, password).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JWT token and user information.</returns>
    /// <response code="200">Login successful — returns token and user data.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.LoginAsync(request, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : Unauthorized(new { error = result.Error });
    }
}
