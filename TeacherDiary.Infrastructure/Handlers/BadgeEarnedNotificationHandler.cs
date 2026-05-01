using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class BadgeEarnedNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<BadgeEarnedEvent>
{
    public async Task HandleAsync(BadgeEarnedEvent e, CancellationToken cancellationToken)
    {
        var student = await db.Students
            .Where(s => s.Id == e.StudentId)
            .Select(s => new { s.UserId, s.ParentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null) return;

        var message = $"Получихте медал: {e.BadgeName}.";

        if (student.UserId.HasValue)
            await notificationService.CreateAsync(
                student.UserId.Value,
                NotificationType.BadgeEarned,
                message,
                "/student/badges",
                null,
                cancellationToken);

        if (student.ParentId.HasValue)
            await notificationService.CreateAsync(
                student.ParentId.Value,
                NotificationType.BadgeEarned,
                message,
                $"/parent/students/{e.StudentId}",
                null,
                cancellationToken);
    }
}
