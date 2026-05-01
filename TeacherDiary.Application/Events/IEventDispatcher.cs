namespace TeacherDiary.Application.Events;

public interface IEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent;
}
