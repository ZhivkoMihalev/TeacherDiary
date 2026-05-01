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
    /// Challenges are time-bound reading or activity goals assigned to an entire class.
    /// Supported target types:
    /// - <c>Pages</c> — read a certain number of pages
    /// - <c>Books</c> — finish a certain number of books
    /// - <c>Assignments</c> — complete a certain number of assignments
    ///
    /// On creation, progress rows (NotStarted) are automatically created for every active student
    /// in the class. Progress is updated automatically as students log reading or assignment activity —
    /// no manual update from parents is required for challenges.
    /// A corresponding LearningActivity entry is also created in the unified tracking engine.
    /// </remarks>
    /// <param name="classId">ID of the class the challenge is assigned to.</param>
    /// <param name="request">Challenge data (title, description, target type, target value, start/end dates).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created challenge.</returns>
    /// <response code="200">Challenge created — returns <c>{ challengeId }</c>.</response>
    /// <response code="400">Validation error or class not found.</response>
    [HttpPost("api/classes/{classId:guid}/challenges")]
    public async Task<IActionResult> Create(
        Guid classId,
        [FromBody] ChallengeCreateRequest request,
        CancellationToken cancellationToken)
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
    /// Returns the full list of challenges for the class, including active, completed, and past challenges.
    /// Each item includes:
    /// - title, description, target type and value
    /// - start and end dates
    /// - completion statistics (how many students have met the goal)
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of challenges with per-student completion statistics.</returns>
    /// <response code="200">Returns the list of challenges.</response>
    /// <response code="400">Class not found or does not belong to the current teacher.</response>
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

    /// <summary>
    /// Returns per-student progress for a specific challenge.
    /// </summary>
    /// <remarks>
    /// For each active student in the class, returns:
    /// - student name
    /// - current progress value and target value
    /// - status (NotStarted, InProgress, Completed)
    /// - last updated timestamp
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="challengeId">ID of the challenge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of per-student challenge progress entries.</returns>
    /// <response code="200">Returns the student progress list.</response>
    /// <response code="400">Class or challenge not found.</response>
    [HttpGet("api/classes/{classId:guid}/challenges/{challengeId:guid}/student-progress")]
    public async Task<IActionResult> GetStudentProgress(
        Guid classId,
        Guid challengeId,
        CancellationToken cancellationToken)
    {
        var result = await challenges.GetStudentProgressAsync(classId, challengeId, cancellationToken);
        return result.Success ? Ok(result.Data) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Extends the end date of a challenge.
    /// </summary>
    /// <remarks>
    /// Allows the teacher to push back the challenge deadline.
    /// The new end date must be later than the current end date.
    /// Student progress is not affected by this update.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="challengeId">ID of the challenge to update.</param>
    /// <param name="request">Updated deadline data (new end date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Deadline extended successfully.</response>
    /// <response code="400">Class or challenge not found, or new date is not later than the current end date.</response>
    [HttpPatch("api/classes/{classId:guid}/challenges/{challengeId:guid}")]
    public async Task<IActionResult> ExtendDeadline(
        Guid classId,
        Guid challengeId,
        [FromBody] ExtendChallengeDeadlineRequest request,
        CancellationToken cancellationToken)
    {
        var result = await challenges.ExtendChallengeDeadlineAsync(classId, challengeId, request, cancellationToken);
        return result.Success
            ? Ok()
            : BadRequest(new { error = result.Error });
    }
}
