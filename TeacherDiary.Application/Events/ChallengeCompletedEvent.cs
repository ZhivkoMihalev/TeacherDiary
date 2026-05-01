namespace TeacherDiary.Application.Events;

public sealed record ChallengeCompletedEvent(
    Guid StudentId,
    Guid ChallengeId,
    Guid ClassId) : IDomainEvent;
