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
    /// Returns the analytics dashboard for a class.
    /// </summary>
    /// <remarks>
    /// Aggregates class-wide statistics for the teacher dashboard. The response includes:
    /// - student counts (total, active today, inactive today)
    /// - total pages read and assignments completed in the last 7 days
    /// - count of active and recently completed LearningActivities
    /// - top-5 leaderboard by total points
    /// - top-5 readers by pages read in the last 7 days
    /// - top-5 students by best streak
    /// - recent badges awarded in the last 7 days
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Aggregated dashboard data for the class.</returns>
    /// <response code="200">Returns the dashboard data.</response>
    /// <response code="404">Class not found or does not belong to the current teacher.</response>
    [HttpGet("api/classes/{classId:guid}/dashboard")]
    public async Task<IActionResult> GetClassDashboard(Guid classId, CancellationToken cancellationToken)
    {
        var result = await dashboard.GetClassDashboardAsync(classId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns today's activity summary for each student in a class.
    /// </summary>
    /// <remarks>
    /// For every active student in the class, returns:
    /// - pages read today
    /// - assignments completed today
    /// - whether the student was active at all today
    /// - timestamp of their most recent activity (any date)
    ///
    /// Results are sorted descending by pages read today.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Per-student activity summary for today.</returns>
    /// <response code="200">Returns the activity list.</response>
    /// <response code="404">Class not found or does not belong to the current teacher.</response>
    [HttpGet("api/classes/{classId:guid}/students/activity")]
    public async Task<IActionResult> GetClassStudentsActivity(Guid classId, CancellationToken cancellationToken)
    {
        var result = await dashboard.GetClassStudentActivityAsync(classId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns the full points leaderboard for a class.
    /// </summary>
    /// <remarks>
    /// Returns all active students in the class ranked by their total accumulated points,
    /// descending. Points are earned by reading pages and completing assignments.
    /// For the top-5 summary, use the <c>leaderboard</c> field on the dashboard endpoint instead.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked list of students with their total points.</returns>
    /// <response code="200">Returns the leaderboard.</response>
    /// <response code="400">Class not found or does not belong to the current teacher.</response>
    [HttpGet("api/classes/{classId:guid}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(Guid classId, CancellationToken cancellationToken)
    {
        var result = await gamificationService.GetLeaderboardAsync(classId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }
}
