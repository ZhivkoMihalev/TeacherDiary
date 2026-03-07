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
    /// Creates a new assignment for a specific class.
    /// </summary>
    /// <remarks>
    /// This endpoint allows a teacher to create a new assignment that will be automatically
    /// assigned to all students in the class. Progress rows for each student are created automatically.
    /// </remarks>
    /// <param name="classId">The unique identifier of the class.</param>
    /// <param name="request">Assignment creation data (title, description, subject, due date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created assignment.</returns>
    /// <response code="200">Assignment created successfully</response>
    /// <response code="400">Invalid request</response>
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
    /// Returns all assignments created for a specific class.
    /// </summary>
    /// <remarks>
    /// Used by the teacher dashboard to display all assignments assigned to the class.
    /// 
    /// The response includes basic assignment information such as:
    /// - title
    /// - subject
    /// - due date
    /// 
    /// It also provides aggregated statistics showing how many students
    /// have completed the assignment.
    /// </remarks>
    /// <param name="classId">The unique identifier of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of assignments assigned to the class.</returns>
    /// <response code="200">Assignments returned successfully</response>
    /// <response code="404">Class not found</response>
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
}
