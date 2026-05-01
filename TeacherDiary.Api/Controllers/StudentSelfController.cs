using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Reading;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize(Roles = "Student")]
[Route("api/student")]
public class StudentSelfController(IStudentSelfService studentSelf) : ControllerBase
{
    /// <summary>
    /// Returns the authenticated student's own profile and progress.
    /// </summary>
    /// <remarks>
    /// Returns comprehensive data for the logged-in student, including:
    /// - name and active status
    /// - current reading progress for all assigned books
    /// - assignment and challenge progress
    /// - streak and total points
    /// - recent activity log
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive student detail view.</returns>
    /// <response code="200">Returns student details.</response>
    /// <response code="404">Student profile not found for the authenticated user.</response>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyDetails(CancellationToken cancellationToken)
    {
        var result = await studentSelf.GetMyDetailsAsync(cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Updates the authenticated student's reading progress for an assigned book.
    /// </summary>
    /// <remarks>
    /// Advances the student's current page for the given assigned book.
    /// On success, this also:
    /// - logs a ReadingProgress activity entry
    /// - awards gamification points proportional to pages read
    /// - updates the student's streak
    /// - checks for and awards any newly unlocked badges
    /// - auto-increments progress on any active Pages or Books challenges
    ///
    /// If the new page equals or exceeds the book's total pages, the book is marked Completed.
    /// </remarks>
    /// <param name="assignedBookId">ID of the assigned book record.</param>
    /// <param name="request">Update data containing the new current page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Progress updated successfully.</response>
    /// <response code="400">Assigned book not found or validation error.</response>
    [HttpPatch("me/reading/{assignedBookId:guid}")]
    public async Task<IActionResult> UpdateReadingProgress(
        Guid assignedBookId,
        [FromBody] UpdateReadingProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await studentSelf.UpdateReadingProgressAsync(assignedBookId, request.CurrentPage, cancellationToken);
        return result.Success 
            ? Ok() 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Marks an assignment as in progress for the authenticated student.
    /// </summary>
    /// <remarks>
    /// Transitions the student's assignment status from <c>NotStarted</c> to <c>InProgress</c>.
    /// </remarks>
    /// <param name="assignmentId">ID of the assignment to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Assignment marked as in progress.</response>
    /// <response code="400">Assignment not found or student is not enrolled in the assignment's class.</response>
    [HttpPatch("me/assignments/{assignmentId:guid}/start")]
    public async Task<IActionResult> StartAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await studentSelf.StartAssignmentAsync(assignmentId, cancellationToken);
        return result.Success 
            ? Ok() 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Marks an assignment as completed for the authenticated student.
    /// </summary>
    /// <remarks>
    /// Transitions the assignment status to <c>Completed</c> and triggers:
    /// - an AssignmentCompleted activity log entry
    /// - gamification point award
    /// - streak update
    /// - badge evaluation
    /// - challenge progress increment for active Assignments challenges
    /// </remarks>
    /// <param name="assignmentId">ID of the assignment to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Assignment marked as completed.</response>
    /// <response code="400">Assignment not found or student is not enrolled in the assignment's class.</response>
    [HttpPatch("me/assignments/{assignmentId:guid}/complete")]
    public async Task<IActionResult> CompleteAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var result = await studentSelf.CompleteAssignmentAsync(assignmentId, cancellationToken);
        return result.Success 
            ? Ok() 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Marks a challenge as in progress for the authenticated student.
    /// </summary>
    /// <remarks>
    /// Transitions the student's challenge progress from <c>NotStarted</c> to <c>InProgress</c>.
    /// </remarks>
    /// <param name="challengeId">ID of the challenge to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Challenge marked as in progress.</response>
    /// <response code="400">Challenge not found or student is not enrolled in the challenge's class.</response>
    [HttpPatch("me/challenges/{challengeId:guid}/start")]
    public async Task<IActionResult> StartChallenge(Guid challengeId, CancellationToken cancellationToken)
    {
        var result = await studentSelf.StartChallengeAsync(challengeId, cancellationToken);
        return result.Success 
            ? Ok() 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Marks a challenge as completed for the authenticated student.
    /// </summary>
    /// <remarks>
    /// Transitions the challenge status to <c>Completed</c> and triggers:
    /// - gamification point award
    /// - badge evaluation
    /// </remarks>
    /// <param name="challengeId">ID of the challenge to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Challenge marked as completed.</response>
    /// <response code="400">Challenge not found or student is not enrolled in the challenge's class.</response>
    [HttpPatch("me/challenges/{challengeId:guid}/complete")]
    public async Task<IActionResult> CompleteChallenge(Guid challengeId, CancellationToken cancellationToken)
    {
        var result = await studentSelf.CompleteChallengeAsync(challengeId, cancellationToken);
        return result.Success 
            ? Ok() 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns all badges earned by the authenticated student.
    /// </summary>
    /// <remarks>
    /// Returns every badge the student has unlocked, including:
    /// - badge name and description
    /// - icon/image URL
    /// - date earned
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of earned badges.</returns>
    /// <response code="200">Returns the badge list.</response>
    /// <response code="404">Student profile not found for the authenticated user.</response>
    [HttpGet("me/badges")]
    public async Task<IActionResult> GetMyBadges(CancellationToken cancellationToken)
    {
        var result = await studentSelf.GetMyBadgesAsync(cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : NotFound(new { error = result.Error });
    }
}