namespace TeacherDiary.Application.Events;

public sealed record BookOverdueEvent(
    Guid AssignedBookId,
    Guid ClassId,
    string BookTitle) : IDomainEvent;
