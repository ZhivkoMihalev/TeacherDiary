using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Challenges;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
public class ChallengesController(IChallengeService challenges) : ControllerBase
{
    /// <summary>
    /// Creates a new challenge for a class.
    /// </summary>
    /// <remarks>
    /// Challenges allow teachers to create goals such as:
    /// - read X pages
    /// - complete X assignments
    /// - read X books
    /// 
    /// Progress rows are created automatically for each student in the class.
    /// </remarks>
    /// <param name="classId">The class identifier.</param>
    /// <param name="request">Challenge creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created challenge.</returns>
    [HttpPost("api/classes/{classId:guid}/challenges")]
    public async Task<IActionResult> Create(Guid classId, [FromBody] ChallengeCreateRequest request, CancellationToken cancellationToken)
    {
        var result = await challenges.CreateChallengeAsync(classId, request, cancellationToken);
        return result.Success 
            ? Ok(new { challengeId = result.Data }) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns all challenges assigned to a class.
    /// </summary>
    /// <remarks>
    /// Used by the teacher dashboard to display active and past challenges.
    /// 
    /// Each challenge represents a goal that students should achieve,
    /// such as:
    /// - reading a certain number of pages
    /// - completing assignments
    /// - finishing a number of books
    ///
    /// The response also includes aggregated statistics,
    /// such as how many students have completed the challenge.
    /// </remarks>
    /// <param name="classId">The class identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of challenges assigned to the class.</returns>
    [HttpGet("api/classes/{classId:guid}/challenges")]
    public async Task<IActionResult> GetChallenges(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var result = await challenges.GetChallengesAsync(classId, cancellationToken);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }
}
