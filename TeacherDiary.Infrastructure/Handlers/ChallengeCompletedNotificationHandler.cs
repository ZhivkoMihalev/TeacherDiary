using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class ChallengeCompletedNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<ChallengeCompletedEvent>
{
    public async Task HandleAsync(ChallengeCompletedEvent e, CancellationToken cancellationToken)
    {
        var teacherId = await db.Classes
            .Where(c => c.Id == e.ClassId)
            .Select(c => c.TeacherId)
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherId == Guid.Empty) return;

        var student = await db.Students
            .Where(s => s.Id == e.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(cancellationToken);

        var challengeTitle = await db.Challenges
            .Where(c => c.Id == e.ChallengeId)
            .Select(c => c.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null || challengeTitle is null) return;

        var message = $"{student.FirstName} {student.LastName} завърши предизвикателството: {challengeTitle}.";

        await notificationService.CreateAsync(
            teacherId,
            NotificationType.ChallengeCompleted,
            message,
            $"/teacher/classes/{e.ClassId}/challenges",
            null,
            cancellationToken);
    }
}
