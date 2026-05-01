using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class BookOverdueNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<BookOverdueEvent>
{
    public async Task HandleAsync(BookOverdueEvent e, CancellationToken cancellationToken)
    {
        var message = $"Времето за прочитане на книга: {e.BookTitle} изтече.";

        var teacherId = await db.Classes
            .Where(c => c.Id == e.ClassId)
            .Select(c => c.TeacherId)
            .FirstOrDefaultAsync(cancellationToken);

        if (teacherId != Guid.Empty)
            await notificationService.CreateAsync(
                teacherId,
                NotificationType.BookOverdue,
                message,
                $"/teacher/classes/{e.ClassId}/reading",
                e.AssignedBookId,
                cancellationToken);

        var students = await db.Students
            .Where(s => s.ClassId == e.ClassId)
            .Select(s => new { s.Id, s.UserId, s.ParentId })
            .ToListAsync(cancellationToken);

        foreach (var student in students)
        {
            if (student.UserId.HasValue)
                await notificationService.CreateAsync(
                    student.UserId.Value,
                    NotificationType.BookOverdue,
                    message,
                    "/student/dashboard",
                    e.AssignedBookId,
                    cancellationToken);

            if (student.ParentId.HasValue)
                await notificationService.CreateAsync(
                    student.ParentId.Value,
                    NotificationType.BookOverdue,
                    message,
                    $"/parent/students/{student.Id}",
                    e.AssignedBookId,
                    cancellationToken);
        }
    }
}
