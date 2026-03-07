using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Reading;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Tags("Reading")]
[Authorize(Roles = "Teacher")]
[Route("api/reading")]
public class ReadingController(IReadingService readingService) : ControllerBase
{
    /// <summary>
    /// Assigns a book to a class.
    /// </summary>
    /// <remarks>
    /// Automatically creates reading progress rows for all students in the class.
    /// </remarks>
    [HttpPost("{classId:guid}/assigned-books")]
    public async Task<IActionResult> AssignBook(
        Guid classId, 
        [FromBody] AssignBookRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await readingService.AssignBookToClassAsync(classId, request, cancellationToken);
        return result.Success 
            ? Ok(new { assignedBookId = result.Data }) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns all books assigned to a specific class.
    /// </summary>
    /// <remarks>
    /// Used by the teacher dashboard to display all reading assignments for the class.
    /// 
    /// The response includes:
    /// - book title and author
    /// - assignment dates
    /// - reading statistics
    /// 
    /// Aggregated statistics show how many students are currently reading the book
    /// and how many have completed it.
    /// </remarks>
    /// <param name="classId">The unique identifier of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of books assigned to the class.</returns>
    /// <response code="200">Assigned books returned successfully</response>
    /// <response code="400">Class not found or invalid request</response>
    [HttpGet("{classId:guid}/books")]
    public async Task<IActionResult> GetClassBooks(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var result = await readingService.GetAssignedBooksAsync(classId, cancellationToken);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }
}