using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Assignments;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
public class AssignmentsController(IAssignmentService assignments) : ControllerBase
{
    /// <summary>
    /// Creates a new assignment for a class.
    /// </summary>
    /// <remarks>
    /// On creation, progress rows (NotStarted) are automatically created for every active student
    /// in the class, and a corresponding LearningActivity entry is added to the unified tracking engine.
    /// Progress updates are submitted by parents via <c>PATCH /api/parent/students/{studentId}/assignments/{assignmentId}</c>.
    /// </remarks>
    /// <param name="classId">ID of the class the assignment belongs to.</param>
    /// <param name="request">Assignment data (title, description, subject, due date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created assignment.</returns>
    /// <response code="200">Assignment created — returns <c>{ assignmentId }</c>.</response>
    /// <response code="400">Validation error or class not found.</response>
    [HttpPost("api/classes/{classId:guid}/assignments")]
    public async Task<IActionResult> Create(
        Guid classId,
        [FromBody] AssignmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await assignments.CreateAssignmentAsync(classId, request, cancellationToken);
        return result.Success
            ? Ok(new { assignmentId = result.Data })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates an existing assignment.
    /// </summary>
    /// <remarks>
    /// Allows editing the title, description, subject, or due date of an assignment.
    /// Student progress rows are not affected by this update.
    /// </remarks>
    /// <param name="classId">ID of the class the assignment belongs to.</param>
    /// <param name="assignmentId">ID of the assignment to update.</param>
    /// <param name="request">Updated assignment data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Assignment updated successfully.</response>
    /// <response code="400">Class or assignment not found, or validation error.</response>
    [HttpPatch("api/classes/{classId:guid}/assignments/{assignmentId:guid}")]
    public async Task<IActionResult> Update(
        Guid classId,
        Guid assignmentId,
        [FromBody] AssignmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await assignments.UpdateAssignmentAsync(classId, assignmentId, request, cancellationToken);
        return result.Success
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns all assignments for a class.
    /// </summary>
    /// <remarks>
    /// Returns a list of assignments created for the class, each with:
    /// - title, subject, description
    /// - due date
    /// - completion statistics (how many students have submitted)
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of assignments with per-class completion statistics.</returns>
    /// <response code="200">Returns the list of assignments.</response>
    /// <response code="404">Class not found or does not belong to the current teacher.</response>
    [HttpGet("api/classes/{classId:guid}/assignments")]
    public async Task<IActionResult> GetAssignments(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var result = await assignments.GetAssignmentsByClassAsync(classId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(result.Error);
    }

    /// <summary>
    /// Returns per-student progress for a specific assignment.
    /// </summary>
    /// <remarks>
    /// For each active student in the class, returns:
    /// - student name
    /// - assignment status (NotStarted, InProgress, Completed)
    /// - last updated timestamp
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="assignmentId">ID of the assignment.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of per-student assignment progress entries.</returns>
    /// <response code="200">Returns the student progress list.</response>
    /// <response code="404">Class or assignment not found.</response>
    [HttpGet("api/classes/{classId:guid}/assignments/{assignmentId:guid}/students")]
    public async Task<IActionResult> GetStudentProgress(
        Guid classId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await assignments.GetStudentProgressForAssignmentAsync(classId, assignmentId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(result.Error);
    }
}
