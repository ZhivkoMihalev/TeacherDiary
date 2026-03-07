using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Assignments;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IAssignmentService
{
    Task<Result<Guid>> CreateAssignmentAsync(Guid classId, AssignmentCreateRequest request, CancellationToken cancellationToken);

    Task<Result<bool>> UpdateProgressAsync(Guid studentId, Guid assignmentId, bool completed, CancellationToken cancellationToken);

    Task<Result<List<AssignmentListDto>>> GetAssignmentsByClassAsync(Guid classId, CancellationToken cancellationToken);
}
