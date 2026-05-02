using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Messages;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IMessageService
{
    Task<Result<List<ConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken);
    Task<Result<List<MessageDto>>> GetConversationAsync(Guid otherUserId, CancellationToken cancellationToken);
    Task<Result<Guid>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken);
    Task<Result<int>> GetUnreadCountAsync(CancellationToken cancellationToken);
    Task<Result<List<MessageContactDto>>> GetContactsAsync(CancellationToken cancellationToken);
}
