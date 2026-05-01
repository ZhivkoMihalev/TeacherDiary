using Microsoft.Extensions.DependencyInjection;
using TeacherDiary.Application.Events;

namespace TeacherDiary.Infrastructure.Events;

public sealed class EventDispatcher(IServiceProvider serviceProvider) : IEventDispatcher
{
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(domainEvent, cancellationToken);
    }
}
