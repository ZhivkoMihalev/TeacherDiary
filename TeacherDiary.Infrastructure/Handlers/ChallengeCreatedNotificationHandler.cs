using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class ChallengeCreatedNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<ChallengeCreatedEvent>
{
    public async Task HandleAsync(ChallengeCreatedEvent e, CancellationToken cancellationToken)
    {
        var students = await db.Students
            .Where(s => s.ClassId == e.ClassId)
            .Select(s => new { s.Id, s.UserId, s.ParentId })
            .ToListAsync(cancellationToken);

        var message = $"Ново предизвикателство: {e.Title}.";

        foreach (var student in students)
        {
            if (student.UserId.HasValue)
                await notificationService.CreateAsync(
                    student.UserId.Value,
                    NotificationType.ChallengeCreated,
                    message,
                    "/student/dashboard",
                    null,
                    cancellationToken);

            if (student.ParentId.HasValue)
                await notificationService.CreateAsync(
                    student.ParentId.Value,
                    NotificationType.ChallengeCreated,
                    message,
                    $"/parent/students/{student.Id}",
                    null,
                    cancellationToken);
        }
    }
}
