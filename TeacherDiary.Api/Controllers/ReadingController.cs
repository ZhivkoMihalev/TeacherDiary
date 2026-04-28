using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Book;
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
    /// Links a book from the catalog to a class for a given date range.
    /// On creation, reading progress rows (NotStarted, page 0) are automatically created
    /// for every active student currently enrolled in the class.
    /// A LearningActivity entry is also added to the unified tracking engine.
    ///
    /// The book must already exist in the catalog — use <c>POST /api/books</c> to add new books.
    /// </remarks>
    /// <param name="classId">ID of the class to assign the book to.</param>
    /// <param name="request">Assignment data (book ID, start date, end date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created assigned-book record.</returns>
    /// <response code="200">Book assigned — returns <c>{ assignedBookId }</c>.</response>
    /// <response code="400">Book or class not found, or validation error.</response>
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
    /// Returns all books assigned to a class.
    /// </summary>
    /// <remarks>
    /// Returns every book currently assigned to the class, each with:
    /// - book title, author, and total pages
    /// - assignment start and end dates
    /// - reading statistics: number of students not started / reading / completed
    ///
    /// Use <c>GET /api/students/{studentId}/details</c> for per-student reading progress.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of assigned books with class-wide reading statistics.</returns>
    /// <response code="200">Returns the list of assigned books.</response>
    /// <response code="400">Class not found or does not belong to the current teacher.</response>
    [HttpGet("{classId:guid}/assigned-books/{assignedBookId:guid}/students")]
    public async Task<IActionResult> GetStudentProgress(
        Guid classId,
        Guid assignedBookId,
        CancellationToken cancellationToken)
    {
        var result = await readingService.GetStudentProgressForBookAsync(classId, assignedBookId, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }

    [HttpDelete("{classId:guid}/assigned-books/{assignedBookId:guid}")]
    public async Task<IActionResult> RemoveAssignedBook(
        Guid classId,
        Guid assignedBookId,
        CancellationToken cancellationToken)
    {
        var result = await readingService.RemoveAssignedBookAsync(classId, assignedBookId, cancellationToken);
        return result.Success
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

    [HttpPatch("{classId:guid}/assigned-books/{assignedBookId:guid}")]
    public async Task<IActionResult> UpdateAssignedBook(
        Guid classId,
        Guid assignedBookId,
        [FromBody] UpdateAssignedBookRequest request,
        CancellationToken cancellationToken)
    {
        var result = await readingService.UpdateAssignedBookAsync(classId, assignedBookId, request, cancellationToken);
        return result.Success
            ? Ok()
            : BadRequest(new { error = result.Error });
    }

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
