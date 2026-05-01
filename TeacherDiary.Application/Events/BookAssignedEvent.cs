namespace TeacherDiary.Application.Events;

public sealed record BookAssignedEvent(
    Guid AssignedBookId,
    Guid ClassId,
    string BookTitle) : IDomainEvent;
