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
    /// Returns per-student reading progress for an assigned book.
    /// </summary>
    /// <remarks>
    /// For each active student in the class, returns:
    /// - student name
    /// - current page and total pages
    /// - status (NotStarted, InProgress, Completed)
    /// - last updated timestamp
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="assignedBookId">ID of the assigned book record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of per-student reading progress entries.</returns>
    /// <response code="200">Returns the reading progress list.</response>
    /// <response code="400">Class or assigned book not found.</response>
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

    /// <summary>
    /// Removes a book assignment from a class.
    /// </summary>
    /// <remarks>
    /// Deletes the assigned book record and all associated student reading progress rows.
    /// This does not delete the book from the catalog — it only removes the class-level assignment.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="assignedBookId">ID of the assigned book record to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Assignment removed successfully.</response>
    /// <response code="400">Class or assigned book not found.</response>
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

    /// <summary>
    /// Updates the date range of an assigned book.
    /// </summary>
    /// <remarks>
    /// Allows changing the start and/or end date of an existing book assignment.
    /// Student reading progress rows are not affected by this update.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="assignedBookId">ID of the assigned book record to update.</param>
    /// <param name="request">Updated assignment data (start date, end date).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Assignment updated successfully.</response>
    /// <response code="400">Class or assigned book not found, or validation error.</response>
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

    /// <summary>
    /// Returns a summary list of all books assigned to a class.
    /// </summary>
    /// <remarks>
    /// Returns every book currently assigned to the class. Each entry includes:
    /// - book title, author, and total pages
    /// - assignment start and end dates
    /// - reading status summary (not started / in progress / completed counts)
    ///
    /// For per-student reading progress on a specific book, use
    /// <c>GET /api/reading/{classId}/assigned-books/{assignedBookId}/students</c>.
    /// </remarks>
    /// <param name="classId">ID of the class.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of assigned books with class-wide reading statistics.</returns>
    /// <response code="200">Returns the list of assigned books.</response>
    /// <response code="400">Class not found or does not belong to the current teacher.</response>
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
