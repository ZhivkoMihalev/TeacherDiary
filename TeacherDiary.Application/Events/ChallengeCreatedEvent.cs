namespace TeacherDiary.Application.Events;

public sealed record ChallengeCreatedEvent(
    Guid ChallengeId,
    Guid ClassId,
    string Title) : IDomainEvent;
