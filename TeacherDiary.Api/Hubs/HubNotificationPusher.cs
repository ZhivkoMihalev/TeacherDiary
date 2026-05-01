using Microsoft.AspNetCore.SignalR;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Notifications;

namespace TeacherDiary.Api.Hubs;

public sealed class HubNotificationPusher(IHubContext<NotificationHub> hub) : INotificationPusher
{
    public async Task PushAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken)
    {
        await hub.Clients.Group(userId.ToString())
            .SendAsync("ReceiveNotification", notification, cancellationToken);
    }
}
