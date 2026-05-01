namespace TeacherDiary.Application.DTOs.Messages;

public class SendMessageRequest
{
    public Guid ReceiverId { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
}
