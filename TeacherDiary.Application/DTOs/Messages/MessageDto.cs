namespace TeacherDiary.Application.DTOs.Messages;

public class MessageDto
{
    public Guid Id { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFromMe { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}
