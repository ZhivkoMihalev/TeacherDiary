using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Reading;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize(Roles = "Teacher")]
[Route("api/books")]
[Tags("Books")]
public class BooksController(IReadingService readingService) : ControllerBase
{
    /// <summary>
    /// Returns all books available in the catalog.
    /// </summary>
    /// <remarks>
    /// Returns the global book catalog. Books are shared across all organizations.
    /// Use this endpoint to browse available books before assigning one to a class via
    /// <c>POST /api/reading/{classId}/assigned-books</c>.
    /// Results can be filtered by <paramref name="gradeLevel"/>.
    /// </remarks>
    /// <param name="gradeLevel">Optional grade level filter (e.g. 3 for third grade).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of books matching the filter.</returns>
    /// <response code="200">Returns the list of books.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpGet]
    public async Task<IActionResult> GetBooks(
        [FromQuery] int? gradeLevel,
        CancellationToken cancellationToken)
    {
        var result = await readingService.GetBooksAsync(gradeLevel, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Adds a new book to the catalog.
    /// </summary>
    /// <remarks>
    /// Creates a book entry in the global catalog so it can be assigned to classes.
    /// Provide <c>TotalPages</c> so that reading progress percentages and completion
    /// detection work correctly when students log their current page.
    /// </remarks>
    /// <param name="request">Book data (title, author, grade level, total pages).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created book.</returns>
    /// <response code="200">Book created — returns <c>{ bookId }</c>.</response>
    /// <response code="400">Validation error.</response>
    [HttpPost]
    public async Task<IActionResult> CreateBook(
        [FromBody] BookCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await readingService.CreateBookAsync(request, cancellationToken);
        return result.Success
            ? Ok(new { bookId = result.Data })
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Updates a book in the catalog.
    /// </summary>
    /// <remarks>
    /// Allows editing the title, author, grade level, or total pages of an existing book.
    /// Changes to total pages affect future progress percentage calculations but do not
    /// retroactively change students' completed status.
    /// </remarks>
    /// <param name="bookId">ID of the book to update.</param>
    /// <param name="request">Updated book data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Empty response on success.</returns>
    /// <response code="200">Book updated successfully.</response>
    /// <response code="404">Book not found.</response>
    [HttpPatch("{bookId:guid}")]
    public async Task<IActionResult> UpdateBook(
        Guid bookId,
        [FromBody] BookUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await readingService.UpdateBookAsync(bookId, request, cancellationToken);
        return result.Success
            ? Ok()
            : NotFound(new { error = result.Error });
    }
}
