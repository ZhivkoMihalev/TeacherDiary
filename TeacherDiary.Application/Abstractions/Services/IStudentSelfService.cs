using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Students;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IStudentSelfService
{
    Task<Result<StudentDetailsDto>> GetMyDetailsAsync(CancellationToken cancellationToken);

    Task<Result<bool>> UpdateReadingProgressAsync(Guid assignedBookId, int currentPage, CancellationToken cancellationToken);

    Task<Result<bool>> StartAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<Result<bool>> CompleteAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken);

    Task<Result<bool>> StartChallengeAsync(Guid challengeId, CancellationToken cancellationToken);

    Task<Result<bool>> CompleteChallengeAsync(Guid challengeId, CancellationToken cancellationToken);

    Task<Result<List<StudentBadgeDto>>> GetMyBadgesAsync(CancellationToken cancellationToken);
}