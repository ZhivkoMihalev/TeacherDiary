using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class StudentJoinedClassNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<StudentJoinedClassEvent>
{
    public async Task HandleAsync(StudentJoinedClassEvent e, CancellationToken cancellationToken)
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

        if (student is null) return;

        var message = $"{student.FirstName} {student.LastName} се присъедини към класа.";

        await notificationService.CreateAsync(
            teacherId,
            NotificationType.StudentJoinedClass,
            message,
            $"/teacher/classes/{e.ClassId}/students",
            null,
            cancellationToken);
    }
}
