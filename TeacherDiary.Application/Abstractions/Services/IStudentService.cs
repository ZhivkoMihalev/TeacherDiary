using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Domain.Common;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IStudentService
{
    Task<Result<bool>> AddStudentToClassAsync(Guid classId, Guid studentId, CancellationToken cancellationToken);

    Task<Result<List<StudentDto>>> GetByClassAsync(Guid classId, CancellationToken cancellationToken);

    Task<Result<PagedResult<StudentSearchDto>>> SearchAsync(string name, int page, int pageSize, CancellationToken cancellationToken);

    Task<Result<bool>> RemoveStudentFromClassAsync(Guid studentId, CancellationToken cancellationToken);
}
