namespace TeacherDiary.Application.Events;

public sealed record StreakBrokenEvent(
    Guid StudentId,
    int OldStreak) : IDomainEvent;
