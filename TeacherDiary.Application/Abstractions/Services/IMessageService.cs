using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Messages;

namespace TeacherDiary.Application.Abstractions.Services;

public interface IMessageService
{
    Task<Result<List<ConversationDto>>> GetConversationsAsync(CancellationToken ct);
    Task<Result<List<MessageDto>>> GetConversationAsync(Guid otherUserId, CancellationToken ct);
    Task<Result<Guid>> SendMessageAsync(SendMessageRequest request, CancellationToken ct);
    Task<Result<int>> GetUnreadCountAsync(CancellationToken ct);
    Task<Result<List<MessageContactDto>>> GetContactsAsync(CancellationToken ct);
}
