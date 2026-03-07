using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Tags("Students")]
[Authorize(Roles = "Teacher")]
public class StudentsController(IStudentService studentService, IDashboardService dashboardService) : ControllerBase
{
    /// <summary>
    /// Returns all students in a class.
    /// </summary>
    [HttpGet("api/classes/{classId:guid}/students")]
    public async Task<IActionResult> GetByClass(
        Guid classId, 
        CancellationToken cancellationToken)
    {
        var result = await studentService.GetByClassAsync(classId, cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Returns detailed information about a student.
    /// </summary>
    /// <remarks>
    /// Includes:
    /// - reading progress
    /// - assignment progress
    /// - activity history
    /// </remarks>
    [HttpGet("{studentId:guid}/details")]
    public async Task<IActionResult> GetStudentDetails(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetStudentDetailsAsync(studentId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Adds a student to a class.
    /// </summary>
    [HttpPost("classes/{classId:guid}/students/{studentId:guid}")]
    public async Task<IActionResult> AddStudentToClass(
        Guid classId, 
        Guid studentId, 
        CancellationToken cancellationToken)
    {
        var result = await studentService.AddStudentToClassAsync(classId, studentId, cancellationToken);
        return result.Success
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Searches students by name.
    /// </summary>
    [HttpGet("api/students/search")]
    public async Task<IActionResult> Search(
        [FromQuery] string name,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await studentService.SearchAsync(name, page, pageSize, cancellationToken);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Removes a student from their current class.
    /// </summary>
    [HttpDelete("api/students/{studentId:guid}/class")]
    public async Task<IActionResult> RemoveFromClass(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await studentService.RemoveStudentFromClassAsync(studentId, cancellationToken);
        return result.Success 
            ? Ok() 
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Returns all badges earned by a student.
    /// </summary>
    [HttpGet("api/students/{studentId:guid}/badges")]
    public async Task<IActionResult> GetStudentBadges(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetStudentBadgesAsync(studentId, cancellationToken);

        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }
}
