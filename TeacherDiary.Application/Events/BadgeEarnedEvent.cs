namespace TeacherDiary.Application.Events;

public sealed record BadgeEarnedEvent(
    Guid StudentId,
    string BadgeName) : IDomainEvent;
