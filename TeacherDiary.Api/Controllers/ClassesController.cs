using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Classes;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Tags("Classes")]
[Route("api/classes")]
[Authorize(Roles = "Teacher")]
public class ClassesController(IClassService classes, IReadingService readingService) : ControllerBase
{
    /// <summary>
    /// Returns all classes owned by the current teacher.
    /// </summary>
    /// <remarks>
    /// Only returns classes where <c>TeacherId</c> matches the authenticated teacher
    /// and the class belongs to the teacher's organization.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of classes owned by the teacher.</returns>
    /// <response code="200">Returns the list of classes (may be empty).</response>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await classes.GetMyClassesAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new class.
    /// </summary>
    /// <remarks>
    /// The class is automatically associated with the authenticated teacher's organization.
    /// After creation, students can be enrolled via <c>POST /api/classes/{classId}/students/{studentId}</c>.
    /// </remarks>
    /// <param name="request">Class data (name, grade level).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created class information including its new ID.</returns>
    /// <response code="200">Class created successfully.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] ClassCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await classes.CreateAsync(request, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates a class.
    /// </summary>
    /// <remarks>
    /// Allows editing the name or grade level of an existing class.
    /// Only the teacher who owns the class can update it.
    /// </remarks>
    /// <param name="classId">ID of the class to update.</param>
    /// <param name="request">Updated class data (name, grade level).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Class updated successfully.</response>
    /// <response code="404">Class not found or does not belong to the current teacher.</response>
    [HttpPatch("{classId:guid}")]
    public async Task<IActionResult> Update(
        Guid classId,
        [FromBody] ClassUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await classes.UpdateAsync(classId, request, cancellationToken);
        return result.Success
            ? Ok()
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Deletes a class.
    /// </summary>
    /// <remarks>
    /// Only the teacher who owns the class can delete it. Students enrolled in the class
    /// are not deleted — their <c>ClassId</c> is set to <c>null</c>.
    /// </remarks>
    /// <param name="classId">ID of the class to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Class deleted successfully.</response>
    /// <response code="404">Class not found or does not belong to the current teacher.</response>
    [HttpDelete("{classId:guid}")]
    public async Task<IActionResult> Delete(Guid classId, CancellationToken cancellationToken)
    {
        var result = await classes.DeleteAsync(classId, cancellationToken);
        return result.Success
            ? Ok()
            : NotFound(new { error = result.Error });
    }
}
