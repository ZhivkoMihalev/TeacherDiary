namespace TeacherDiary.Application.Events;

public sealed record AssignmentOverdueEvent(
    Guid AssignmentId,
    Guid ClassId,
    string Title) : IDomainEvent;
