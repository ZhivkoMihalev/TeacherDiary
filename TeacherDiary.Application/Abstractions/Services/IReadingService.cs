using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Book;
using TeacherDiary.Application.DTOs.Reading;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IReadingService
{
    Task<Result<Guid>> CreateBookAsync(BookCreateRequest request, CancellationToken cancellationToken);

    Task<Result<Guid>> AssignBookToClassAsync(Guid classId, AssignBookRequest request, CancellationToken cancellationToken);

    Task<Result<bool>> UpdateProgressAsync(Guid studentId, Guid assignedBookId, int currentPage, CancellationToken cancellationToken);

    Task<Result<List<BookDto>>> GetBooksAsync(int? gradeLevel, CancellationToken cancellationToken);

    Task<Result<List<AssignedBookDto>>> GetAssignedBooksAsync(Guid classId, CancellationToken cancellationToken);
}
