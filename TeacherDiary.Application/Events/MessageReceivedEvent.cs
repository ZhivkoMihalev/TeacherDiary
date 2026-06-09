namespace TeacherDiary.Application.Events;

public sealed record MessageReceivedEvent(Guid MessageId, Guid SenderId, Guid ReceiverId, string SenderName) : IDomainEvent;
