namespace TeacherDiary.Application.Events;

public sealed record StreakReminderEvent(Guid StudentId) : IDomainEvent;
