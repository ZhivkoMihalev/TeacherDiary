using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class BookAssignedNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<BookAssignedEvent>
{
    public async Task HandleAsync(BookAssignedEvent e, CancellationToken cancellationToken)
    {
        var students = await db.Students
            .Where(s => s.ClassId == e.ClassId)
            .Select(s => new { s.Id, s.UserId, s.ParentId })
            .ToListAsync(cancellationToken);

        var message = $"Получихте нова книга за четене: {e.BookTitle}.";

        foreach (var student in students)
        {
            if (student.UserId.HasValue)
                await notificationService.CreateAsync(
                    student.UserId.Value,
                    NotificationType.BookAssigned,
                    message,
                    "/student/dashboard",
                    null,
                    cancellationToken);

            if (student.ParentId.HasValue)
                await notificationService.CreateAsync(
                    student.ParentId.Value,
                    NotificationType.BookAssigned,
                    message,
                    $"/parent/students/{student.Id}",
                    null,
                    cancellationToken);
        }
    }
}
