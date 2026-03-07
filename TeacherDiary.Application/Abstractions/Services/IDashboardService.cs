using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Dashboard;
using TeacherDiary.Application.DTOs.Students;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IDashboardService
{
    Task<Result<DashboardDto>> GetClassDashboardAsync(Guid classId, CancellationToken cancellationToken);

    Task<Result<List<StudentActivityDto>>> GetClassStudentActivityAsync(Guid classId, CancellationToken cancellationToken);

    Task<Result<StudentDetailsDto>> GetStudentDetailsAsync(Guid studentId, CancellationToken cancellationToken);

    Task<Result<List<StudentBadgeDto>>> GetStudentBadgesAsync(Guid studentId, CancellationToken cancellationToken);
}
