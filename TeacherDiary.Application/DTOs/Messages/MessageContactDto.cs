namespace TeacherDiary.Application.DTOs.Messages;

public class MessageContactDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string? StudentName { get; set; }
}
