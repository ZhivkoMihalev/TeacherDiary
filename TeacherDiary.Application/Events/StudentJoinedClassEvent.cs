namespace TeacherDiary.Application.Events;

public sealed record StudentJoinedClassEvent(
    Guid StudentId,
    Guid ClassId) : IDomainEvent;
