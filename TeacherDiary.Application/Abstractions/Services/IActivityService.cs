namespace TeacherDiary.Application.Abstractions.Services;

public interface IActivityService
{
    Task LogReadingAsync(Guid studentId, Guid assignedBookId, int pagesRead, bool bookCompleted, int bookPoints, CancellationToken cancellationToken);

    Task LogAssignmentCompletedAsync(Guid studentId, Guid assignmentId, int points, CancellationToken cancellationToken);

    Task LogChallengeCompletedAsync(Guid studentId, Guid challengeId, int points, CancellationToken cancellationToken);
}
