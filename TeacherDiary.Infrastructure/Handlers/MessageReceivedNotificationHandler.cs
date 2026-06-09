using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;

namespace TeacherDiary.Infrastructure.Handlers;

public sealed class MessageReceivedNotificationHandler(
    INotificationService notificationService) : IDomainEventHandler<MessageReceivedEvent>
{
    public async Task HandleAsync(MessageReceivedEvent e, CancellationToken cancellationToken)
    {
        await notificationService.CreateAsync(
            e.ReceiverId,
            NotificationType.MessageReceived,
            $"Ново съобщение от {e.SenderName}",
            "/messages",
            e.MessageId,
            cancellationToken);
    }
}
