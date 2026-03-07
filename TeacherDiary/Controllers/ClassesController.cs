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
    /// Retrieves all classes created by the current teacher.
    /// </summary>
    /// <returns>List of classes owned by the teacher.</returns>
    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var result = await classes.GetMyClassesAsync(cancellationToken);
        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new class.
    /// </summary>
    /// <param name="request">Class creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created class information.</returns>
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
    /// Deletes a class.
    /// </summary>
    /// <param name="classId">The class identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status of the delete operation.</returns>
    [HttpDelete("{classId:guid}")]
    public async Task<IActionResult> Delete(Guid classId, CancellationToken cancellationToken)
    {
        var result = await classes.DeleteAsync(classId, cancellationToken);
        return result.Success 
            ? Ok() 
            : NotFound(new { error = result.Error });
    }
}
