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
    /// Returns all students belonging to the authenticated parent.
    /// </summary>
    [HttpGet("students")]
    public async Task<IActionResult> GetMyStudents(CancellationToken cancellationToken)
    {
        var result = await parentService.GetMyStudentsAsync(cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns detailed information about a specific student belonging to the authenticated parent.
    /// </summary>
    /// <remarks>
    /// This endpoint allows a parent to retrieve detailed information about one of their children.
    /// 
    /// The returned data may include:
    /// - student basic information
    /// - reading progress
    /// - assignment progress
    /// - activity statistics
    /// 
    /// The parent can only access students linked to their account.
    /// </remarks>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Detailed information about the student.</returns>
    /// <response code="200">Student returned successfully</response>
    /// <response code="404">Student not found or does not belong to the parent</response>
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
    /// Creates a new student profile for the parent.
    /// </summary>
    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await parentService.CreateStudentAsync(request, cancellationToken);

        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Updates reading progress for a student.
    /// </summary>
    [HttpPatch("students/{studentId}/reading/{assignedBookId}")]
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
    /// Updates assignment progress for a student.
    /// </summary>
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

        return result.Success ? Ok() : BadRequest(result.Error);
    }
}
