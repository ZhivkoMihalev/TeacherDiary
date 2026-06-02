using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Analytics;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IAnalyticsService
{
    Task<Result<ClassAnalyticsDto>> GetClassAnalyticsAsync(Guid classId, int days, CancellationToken cancellationToken);
}
