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
    /// Returns all books available in the system.
    /// </summary>
    /// <remarks>
    /// Used when assigning books to a class.
    /// Can be filtered by grade level.
    /// </remarks>
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
    /// Creates a new book.
    /// </summary>
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
}