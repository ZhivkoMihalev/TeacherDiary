namespace TeacherDiary.Application.Abstractions.Services;

public interface IBadgeService
{
    Task EvaluateAsync(Guid studentId, CancellationToken cancellationToken);
}
