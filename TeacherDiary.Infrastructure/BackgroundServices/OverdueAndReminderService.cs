using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.BackgroundServices;

public sealed class OverdueAndReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueAndReminderService> logger) : BackgroundService
{
    private DateOnly _lastStreakReminderDate = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IEventDispatcher>();

                await CheckOverdueAssignmentsAsync(db, dispatcher, stoppingToken);
                await CheckOverdueBooksAsync(db, dispatcher, stoppingToken);
                await CheckStreakRemindersAsync(db, dispatcher, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error in overdue/reminder background service");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckOverdueAssignmentsAsync(AppDbContext db, IEventDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var window = DateTime.UtcNow.AddHours(-1);

        var alreadyNotified = await db.Notifications
            .Where(n => n.Type == NotificationType.AssignmentOverdue && n.ReferenceId != null)
            .Select(n => n.ReferenceId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var overdueAssignments = await db.Assignments
            .Where(a => a.DueDate.HasValue
                        && a.DueDate.Value >= window
                        && a.DueDate.Value <= DateTime.UtcNow
                        && !alreadyNotified.Contains(a.Id))
            .Select(a => new { a.Id, a.Title, a.ClassId })
            .ToListAsync(cancellationToken);

        foreach (var assignment in overdueAssignments)
            await dispatcher.PublishAsync(
                new AssignmentOverdueEvent(assignment.Id, assignment.ClassId, assignment.Title), cancellationToken);
    }

    private async Task CheckOverdueBooksAsync(AppDbContext db, IEventDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var window = DateTime.UtcNow.AddHours(-1);

        var alreadyNotified = await db.Notifications
            .Where(n => n.Type == NotificationType.BookOverdue && n.ReferenceId != null)
            .Select(n => n.ReferenceId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var overdueBooks = await db.AssignedBooks
            .Where(ab => ab.EndDateUtc.HasValue
                         && ab.EndDateUtc.Value >= window
                         && ab.EndDateUtc.Value <= DateTime.UtcNow
                         && !alreadyNotified.Contains(ab.Id))
            .Select(ab => new { ab.Id, ab.ClassId, BookTitle = ab.Book.Title })
            .ToListAsync(cancellationToken);

        foreach (var book in overdueBooks)
            await dispatcher.PublishAsync(
                new BookOverdueEvent(book.Id, book.ClassId, book.BookTitle), cancellationToken);
    }

    private async Task CheckStreakRemindersAsync(AppDbContext db, IEventDispatcher dispatcher, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Send streak reminders once per day, between 19:00 and 20:00 UTC
        if (now.Hour < 19 || now.Hour >= 20 || _lastStreakReminderDate == today)
            return;

        _lastStreakReminderDate = today;

        var studentsNeedingReminder = await db.StudentStreaks
            .Where(s => s.CurrentStreak > 0 && s.LastActiveDate < today)
            .Select(s => s.StudentProfileId)
            .ToListAsync(cancellationToken);

        foreach (var studentId in studentsNeedingReminder)
            await dispatcher.PublishAsync(new StreakReminderEvent(studentId), cancellationToken);
    }
}
