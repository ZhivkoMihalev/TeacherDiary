using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Students;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IParentService
{
    Task<Result<Guid>> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken);

    Task<Result<List<StudentDto>>> GetMyStudentsAsync(CancellationToken cancellationToken);

    Task<Result<StudentDetailsDto>> GetStudentAsync(Guid studentId, CancellationToken cancellationToken);

    Task<Result<bool>> DeleteStudentAsync(Guid studentId, CancellationToken cancellationToken);
}
