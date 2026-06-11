using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class StreakBrokenNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<StreakBrokenEvent>
{
    public async Task HandleAsync(StreakBrokenEvent e, CancellationToken cancellationToken)
    {
        var student = await db.Students
            .Where(s => s.Id == e.StudentId)
            .Select(s => new { s.UserId, s.ParentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null) return;

        var message = $"Your {e.OldStreak}-day streak was broken. Keep studying every day!";

        if (student.UserId.HasValue)
            await notificationService.CreateAsync(
                student.UserId.Value,
                NotificationType.StreakBroken,
                message,
                "/student/dashboard",
                null,
                cancellationToken);

        if (student.ParentId.HasValue)
            await notificationService.CreateAsync(
                student.ParentId.Value,
                NotificationType.StreakBroken,
                message,
                $"/parent/students/{e.StudentId}",
                null,
                cancellationToken);
    }
}
