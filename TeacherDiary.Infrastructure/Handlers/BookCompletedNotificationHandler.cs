using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class BookCompletedNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<BookCompletedEvent>
{
    public async Task HandleAsync(BookCompletedEvent e, CancellationToken cancellationToken)
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

        var bookTitle = await db.AssignedBooks
            .Where(ab => ab.Id == e.AssignedBookId)
            .Select(ab => ab.Book.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null || bookTitle is null) return;

        var message = $"{student.FirstName} {student.LastName} прочете книгата: {bookTitle}.";

        await notificationService.CreateAsync(
            teacherId,
            NotificationType.BookCompleted,
            message,
            $"/teacher/classes/{e.ClassId}/reading",
            null,
            cancellationToken);
    }
}
