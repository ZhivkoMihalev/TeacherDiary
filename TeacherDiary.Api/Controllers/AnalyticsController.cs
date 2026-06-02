using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/classes/{classId:guid}")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>Returns analytics data for a class.</summary>
    /// <param name="classId">ID of the class.</param>
    /// <param name="days">Number of days for the activity timeline (1–90, default 30).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Analytics including activity timeline, student engagement, subject completion, reading stats.</returns>
    /// <response code="200">Returns analytics data.</response>
    /// <response code="404">Class not found or does not belong to the authenticated teacher.</response>
    [HttpGet("analytics")]
    public async Task<IActionResult> GetClassAnalytics(
        Guid classId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await analyticsService.GetClassAnalyticsAsync(classId, days, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }
}
