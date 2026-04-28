using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Classes;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IClassService
{
    Task<Result<ClassDto>> CreateAsync(ClassCreateRequest request, CancellationToken cancellationToken);

    Task<Result<List<ClassDto>>> GetMyClassesAsync(CancellationToken cancellationToken);

    Task<Result<bool>> UpdateAsync(Guid classId, ClassUpdateRequest request, CancellationToken cancellationToken);

    Task<Result<bool>> DeleteAsync(Guid classId, CancellationToken cancellationToken);
}