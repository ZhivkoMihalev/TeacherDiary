using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Tags("Dashboard")]
[Authorize(Roles = "Teacher")]
public class DashboardController(IDashboardService dashboard, IGamificationService gamificationService) : ControllerBase
{
    /// <summary>
    /// Returns analytics dashboard data for a class.
    /// </summary>
    /// <remarks>
    /// Includes:
    /// - active students
    /// - pages read
    /// - completed assignments
    /// - leaderboard
    /// - badges
    /// </remarks>
    [HttpGet("api/classes/{classId:guid}/dashboard")]
    public async Task<IActionResult> GetClassDashboard(Guid classId, CancellationToken cancellationToken)
    {
        var result = await dashboard.GetClassDashboardAsync(classId, cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns activity statistics for students in a class.
    /// </summary>
    /// <remarks>
    /// Used by the teacher dashboard to show daily activity such as:
    /// - pages read today
    /// - assignments completed today
    /// - last activity timestamp
    /// </remarks>
    [HttpGet("api/classes/{classId:guid}/students/activity")]
    public async Task<IActionResult> GetClassStudentsActivity(Guid classId, CancellationToken cancellationToken)
    {
        var result = await dashboard.GetClassStudentActivityAsync(classId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns the leaderboard for a class based on student points.
    /// </summary>
    [HttpGet("api/classes/{classId}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid classId, CancellationToken cancellationToken)
    {
        var result = await gamificationService.GetLeaderboardAsync(classId, cancellationToken);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }
}
