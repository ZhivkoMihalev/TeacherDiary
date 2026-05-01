using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class StreakReminderNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<StreakReminderEvent>
{
    public async Task HandleAsync(StreakReminderEvent e, CancellationToken cancellationToken)
    {
        var student = await db.Students
            .Where(s => s.Id == e.StudentId)
            .Select(s => new { s.UserId, s.ParentId })
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null) return;

        const string message = "Не забравяй! Учи и днес, за да запазиш серията си.";

        if (student.UserId.HasValue)
            await notificationService.CreateAsync(
                student.UserId.Value,
                NotificationType.StreakReminder,
                message,
                "/student/dashboard",
                null,
                cancellationToken);

        if (student.ParentId.HasValue)
            await notificationService.CreateAsync(
                student.ParentId.Value,
                NotificationType.StreakReminder,
                message,
                $"/parent/students/{e.StudentId}",
                null,
                cancellationToken);
    }
}
