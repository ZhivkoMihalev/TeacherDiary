using Moq;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Handlers;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class MessageReceivedNotificationHandlerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock = new();

    private MessageReceivedNotificationHandler CreateHandler() =>
        new(_notificationServiceMock.Object);

    [Fact]
    public async Task HandleAsync_WhenInvoked_CreatesNotificationForReceiver()
    {
        var messageId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();
        var e = new MessageReceivedEvent(messageId, senderId, receiverId, "Jane Parent");
        var handler = CreateHandler();

        await handler.HandleAsync(e, CancellationToken.None);

        _notificationServiceMock.Verify(
            n => n.CreateAsync(
                receiverId,
                NotificationType.MessageReceived,
                "New message from Jane Parent",
                "/messages",
                messageId,
                CancellationToken.None),
            Times.Once);
    }
}
