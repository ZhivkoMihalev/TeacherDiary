using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class AssignmentCompletedNotificationHandler(
    AppDbContext db,
    INotificationService notificationService) : IDomainEventHandler<AssignmentCompletedEvent>
{
    public async Task HandleAsync(AssignmentCompletedEvent e, CancellationToken cancellationToken)
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

        var assignment = await db.Assignments
            .Where(a => a.Id == e.AssignmentId)
            .Select(a => a.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (student is null || assignment is null) return;

        var message = $"{student.FirstName} {student.LastName} завърши задача: {assignment}.";

        await notificationService.CreateAsync(
            teacherId,
            NotificationType.AssignmentCompleted,
            message,
            $"/teacher/classes/{e.ClassId}/assignments",
            null,
            cancellationToken);
    }
}
