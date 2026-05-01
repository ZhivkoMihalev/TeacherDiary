namespace TeacherDiary.Application.Events;

public sealed record BookCompletedEvent(
    Guid StudentId,
    Guid AssignedBookId,
    Guid ClassId) : IDomainEvent;
