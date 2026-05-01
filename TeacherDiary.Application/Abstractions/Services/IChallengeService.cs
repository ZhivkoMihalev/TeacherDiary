using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Challenges;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IChallengeService
{
    Task<Result<Guid>> CreateChallengeAsync(Guid classId, ChallengeCreateRequest request, CancellationToken cancellationToken);

    Task<Result<List<ChallengeDto>>> GetChallengesAsync(Guid classId, CancellationToken cancellationToken);

    Task<Result<bool>> ExtendChallengeDeadlineAsync(Guid classId, Guid challengeId, ExtendChallengeDeadlineRequest request, CancellationToken cancellationToken);

    Task<Result<List<ChallengeStudentProgressDto>>> GetStudentProgressAsync(Guid classId, Guid challengeId, CancellationToken cancellationToken);
}
