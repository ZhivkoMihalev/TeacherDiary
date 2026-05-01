namespace TeacherDiary.Application.Events;

public sealed record AssignmentCreatedEvent(
    Guid AssignmentId,
    Guid ClassId,
    string Title) : IDomainEvent;
