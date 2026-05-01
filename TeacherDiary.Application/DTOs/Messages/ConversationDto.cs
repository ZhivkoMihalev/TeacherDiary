namespace TeacherDiary.Application.DTOs.Messages;

public class ConversationDto
{
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = default!;
    public string? StudentName { get; set; }
    public string LastMessage { get; set; } = default!;
    public bool LastMessageIsImage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
    public bool LastMessageIsFromMe { get; set; }
}
