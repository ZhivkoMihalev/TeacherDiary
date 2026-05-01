namespace TeacherDiary.Application.Events;

public sealed record AssignmentCompletedEvent(
    Guid StudentId,
    Guid AssignmentId,
    Guid ClassId) : IDomainEvent;
