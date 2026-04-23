namespace TeacherDiary.Application.Abstractions.Services;

public interface IActivityService
{
    Task LogReadingAsync(Guid studentId, Guid assignedBookId, int pagesRead, bool bookCompleted, CancellationToken cancellationToken);

    Task LogAssignmentCompletedAsync(Guid studentId, Guid assignmentId, CancellationToken cancellationToken);
}
