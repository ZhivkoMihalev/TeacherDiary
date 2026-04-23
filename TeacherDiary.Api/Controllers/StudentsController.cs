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
    /// Returns all students enrolled in a class.
    /// </summary>
    /// <remarks>
    /// Returns both active and inactive students. Sorted by last name, then first name.
    /// Only accessible to the teacher who owns the class.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of students enrolled in the class.</returns>
    /// <response code="200">Returns the student list.</response>
    /// <response code="404">Class not found or does not belong to the current teacher.</response>
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
    /// Provides a full student profile view for the teacher. Access is restricted to the teacher
    /// whose class the student is enrolled in.
    ///
    /// The response includes:
    /// - student name and active status
    /// - current reading progress for all assigned books
    /// - assignment progress for all class assignments
    /// - activity log summary for the last 7 days (pages read and assignments completed per day)
    /// - overall statistics (total pages read, total assignments completed)
    /// - progress across all LearningActivities (reading, assignments, challenges)
    /// - timestamp of the most recent activity
    /// </remarks>
    /// <param name="studentId">ID of the student.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comprehensive student detail view.</returns>
    /// <response code="200">Returns student details.</response>
    /// <response code="404">Student not found or not in a class owned by the current teacher.</response>
    [HttpGet("api/students/{studentId:guid}/details")]
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
    /// Enrolls a student in a class.
    /// </summary>
    /// <remarks>
    /// Sets the student's <c>ClassId</c> and bootstraps all progress records so the student
    /// starts with a clean state for every existing activity in the class:
    /// - <c>ReadingProgress</c> rows for all assigned books (NotStarted, page 0)
    /// - <c>AssignmentProgress</c> rows for all assignments (NotStarted)
    /// - <c>ChallengeProgress</c> rows for all challenges (value 0)
    /// - <c>StudentLearningActivityProgress</c> rows for all active LearningActivities (NotStarted)
    ///
    /// The student must already exist — parents create student profiles via
    /// <c>POST /api/parent/students</c>. Use <c>GET /api/students/search</c> to find students by name.
    /// </remarks>
    /// <param name="classId">ID of the class to enroll the student in.</param>
    /// <param name="studentId">ID of the student to enroll.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Student enrolled successfully.</response>
    /// <response code="400">Class or student not found.</response>
    [HttpPost("api/classes/{classId:guid}/students/{studentId:guid}")]
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
    /// Searches for students by name.
    /// </summary>
    /// <remarks>
    /// Case-insensitive partial match on first name or last name.
    /// Results are scoped to students who either:
    /// - have no class yet (<c>ClassId = null</c>), or
    /// - are enrolled in a class that belongs to the current teacher's organization
    ///
    /// This is the typical flow for finding a student before enrolling them:
    /// the parent creates the student profile, the teacher searches by name and then calls
    /// <c>POST /api/classes/{classId}/students/{studentId}</c>.
    ///
    /// Results are paginated — use <paramref name="page"/> and <paramref name="pageSize"/> to navigate.
    /// </remarks>
    /// <param name="name">Partial name to search for (min 1 character).</param>
    /// <param name="page">Page number, 1-based (default: 1).</param>
    /// <param name="pageSize">Number of results per page (default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of matching students.</returns>
    /// <response code="200">Returns matching students.</response>
    /// <response code="400">Search error.</response>
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
    /// <remarks>
    /// Sets the student's <c>ClassId</c> to <c>null</c>. The student's historical progress records
    /// (reading, assignments, challenges) are retained and are not deleted.
    /// Only the teacher who owns the student's current class can perform this operation.
    /// </remarks>
    /// <param name="studentId">ID of the student to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Student removed from class successfully.</response>
    /// <response code="400">Student not found, not in a class, or class does not belong to the current teacher.</response>
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
    /// <remarks>
    /// Badges are awarded automatically by the gamification engine when a student meets
    /// specific milestones (e.g. first book read, 7-day streak, 100 pages read).
    /// Access is restricted to the teacher whose class the student is enrolled in.
    /// Results are sorted by award date descending (most recent first).
    /// </remarks>
    /// <param name="studentId">ID of the student.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of badges earned by the student.</returns>
    /// <response code="200">Returns the badge list.</response>
    /// <response code="404">Student not found or not in a class owned by the current teacher.</response>
    [HttpGet("api/students/{studentId:guid}/badges")]
    public async Task<IActionResult> GetStudentBadges(Guid studentId, CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetStudentBadgesAsync(studentId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : NotFound(new { error = result.Error });
    }
}
