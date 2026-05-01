using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Assignments;
using TeacherDiary.Application.DTOs.Reading;
using TeacherDiary.Application.DTOs.Students;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize(Roles = "Parent")]
[Route("api/parent")]
public class ParentsController(
    IParentService parentService,
    IReadingService readingService,
    IAssignmentService assignmentService) : ControllerBase
{
    /// <summary>
    /// Returns all student profiles owned by the authenticated parent.
    /// </summary>
    /// <remarks>
    /// Returns every <c>StudentProfile</c> whose <c>ParentId</c> matches the current user.
    /// A student may not yet be enrolled in a class — <c>ClassId</c> will be <c>null</c> in that case.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of the parent's student profiles.</returns>
    /// <response code="200">Returns the list of students (may be empty).</response>
    /// <response code="404">Unexpected service error.</response>
    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents(CancellationToken cancellationToken)
    {
        var result = await parentService.GetMyStudentsAsync(cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns detailed information about one of the parent's students.
    /// </summary>
    /// <remarks>
    /// Verifies that <paramref name="studentId"/> belongs to the authenticated parent before returning data.
    /// The response includes:
    /// - student name and active status
    /// - current reading progress for all assigned books
    /// - assignment progress for all class assignments
    /// - activity log summary for the last 7 days (pages read and assignments completed per day)
    /// - overall statistics (total pages read, total assignments completed)
    /// - progress across all LearningActivities (reading, assignments, challenges)
    /// - timestamp of the most recent activity
    /// </remarks>
    /// <param name="studentId">ID of the student to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive student detail view.</returns>
    /// <response code="200">Returns student details.</response>
    /// <response code="404">Student not found or does not belong to the authenticated parent.</response>
    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> GetStudent(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await parentService.GetStudentAsync(studentId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Creates a new student profile linked to the authenticated parent.
    /// </summary>
    /// <remarks>
    /// Creates a student with no class assignment (<c>ClassId = null</c>).
    /// To enroll the student in a class, the teacher uses
    /// <c>POST /api/classes/{classId}/students/{studentId}</c> after searching for the student
    /// via <c>GET /api/students/search</c>.
    /// </remarks>
    /// <param name="request">Student data (first name, last name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created student profile.</returns>
    /// <response code="200">Student created — returns the new student ID.</response>
    /// <response code="400">Validation error or authentication failure.</response>
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await parentService.CreateStudentAsync(request, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Deletes a student profile owned by the authenticated parent.
    /// </summary>
    /// <remarks>
    /// Permanently removes the student profile and all associated reading progress,
    /// assignment progress, and activity records.
    /// Only the parent who owns the student can delete the profile.
    /// </remarks>
    /// <param name="studentId">ID of the student profile to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Student deleted successfully.</response>
    /// <response code="400">Student not found or does not belong to the authenticated parent.</response>
    [HttpDelete("students/{studentId:guid}")]
    public async Task<IActionResult> DeleteStudent(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await parentService.DeleteStudentAsync(studentId, cancellationToken);
        return result.Success
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates a student's reading progress for an assigned book.
    /// </summary>
    /// <remarks>
    /// Advances the student's current page for the given assigned book.
    /// On success, this also:
    /// - logs a <c>ReadingProgress</c> activity entry (used for streaks and dashboards)
    /// - awards gamification points proportional to the pages read
    /// - updates the student's streak
    /// - checks for and awards any newly unlocked badges
    /// - auto-increments progress on any active Pages or Books challenges
    ///
    /// If the new page equals or exceeds the book's total pages, the book is marked Completed.
    /// </remarks>
    /// <param name="studentId">ID of the student.</param>
    /// <param name="assignedBookId">ID of the assigned book (not the book catalog ID).</param>
    /// <param name="request">Update data containing the new current page number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Progress updated successfully.</response>
    /// <response code="400">Student or assigned book not found, or validation error.</response>
    [HttpPatch("students/{studentId:guid}/reading/{assignedBookId:guid}")]
    public async Task<IActionResult> UpdateReadingProgress(
        Guid studentId,
        Guid assignedBookId,
        [FromBody] UpdateReadingProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await readingService.UpdateProgressAsync(
            studentId,
            assignedBookId,
            request.CurrentPage,
            cancellationToken);

        return result.Success ? Ok() : BadRequest(result.Error);
    }

    /// <summary>
    /// Marks an assignment as completed (or in progress) for a student.
    /// </summary>
    /// <remarks>
    /// Updates the student's assignment progress.
    /// On success, if <c>MarkCompleted</c> is <c>true</c>, this also:
    /// - logs an <c>AssignmentCompleted</c> activity entry
    /// - awards gamification points for the assignment
    /// - updates the student's streak
    /// - checks for and awards any newly unlocked badges
    /// - auto-increments progress on any active Assignments challenges
    /// </remarks>
    /// <param name="studentId">ID of the student.</param>
    /// <param name="assignmentId">ID of the assignment.</param>
    /// <param name="request">Update data containing whether the assignment is completed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Progress updated successfully.</response>
    /// <response code="400">Student or assignment not found, or validation error.</response>
    [HttpPatch("students/{studentId:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> UpdateAssignmentProgress(
        Guid studentId,
        Guid assignmentId,
        [FromBody] UpdateAssignmentProgressRequest request,
        CancellationToken cancellationToken)
    {
        var result = await assignmentService.UpdateProgressAsync(
            studentId,
            assignmentId,
            request.MarkCompleted,
            cancellationToken);

        return result.Success
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Marks a challenge as in progress for a student.
    /// </summary>
    /// <remarks>
    /// Transitions the student's challenge progress from <c>NotStarted</c> to <c>InProgress</c>.
    /// The student must be enrolled in the class the challenge belongs to.
    /// </remarks>
    /// <param name="studentId">ID of the student.</param>
    /// <param name="challengeId">ID of the challenge to start.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Challenge marked as in progress.</response>
    /// <response code="400">Student or challenge not found, or the student is not enrolled in the challenge's class.</response>
    [HttpPatch("students/{studentId:guid}/challenges/{challengeId:guid}/start")]
    public async Task<IActionResult> StartChallenge(
        Guid studentId,
        Guid challengeId,
        CancellationToken cancellationToken)
    {
        var result = await parentService.StartChallengeForStudentAsync(studentId, challengeId, cancellationToken);
        return result.Success ? Ok() : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Marks a challenge as completed for a student.
    /// </summary>
    /// <remarks>
    /// Transitions the student's challenge progress from <c>InProgress</c> to <c>Completed</c>.
    /// On success, this also awards gamification points and checks for newly unlocked badges.
    /// The student must be enrolled in the class the challenge belongs to.
    /// </remarks>
    /// <param name="studentId">ID of the student.</param>
    /// <param name="challengeId">ID of the challenge to complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Challenge marked as completed.</response>
    /// <response code="400">Student or challenge not found, or the student is not enrolled in the challenge's class.</response>
    [HttpPatch("students/{studentId:guid}/challenges/{challengeId:guid}/complete")]
    public async Task<IActionResult> CompleteChallenge(
        Guid studentId,
        Guid challengeId,
        CancellationToken cancellationToken)
    {
        var result = await parentService.CompleteChallengeForStudentAsync(studentId, challengeId, cancellationToken);
        return result.Success ? Ok() : BadRequest(new { error = result.Error });
    }
}
