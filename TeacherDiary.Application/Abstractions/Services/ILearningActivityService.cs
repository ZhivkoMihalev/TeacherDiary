using TeacherDiary.Domain.Entities;

namespace TeacherDiary.Application.Abstractions.Services;

public interface ILearningActivityService
{
    Task<Guid> CreateForAssignedBookAsync(AssignedBook assignedBook, CancellationToken cancellationToken);

    Task<Guid> CreateForAssignmentAsync(Assignment assignment, CancellationToken cancellationToken);

    Task<Guid> CreateForChallengeAsync(Challenge challenge, CancellationToken cancellationToken);

    Task UpdateReadingProgressAsync(Guid studentId, Guid assignedBookId, int currentPage, CancellationToken cancellationToken);

    Task UpdateAssignmentProgressAsync(Guid studentId, Guid assignmentId, bool completed, int? score, CancellationToken cancellationToken);

    Task UpdateChallengeProgressAsync(Guid studentId, Guid challengeId, int currentValue, bool completed, CancellationToken cancellationToken);
}
