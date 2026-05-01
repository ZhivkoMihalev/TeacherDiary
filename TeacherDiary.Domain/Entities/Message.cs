namespace TeacherDiary.Domain.Entities;

public class Message : BaseEntity
{
    public Guid SenderId { get; set; }

    public Guid ReceiverId { get; set; }

    public string? Content { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsRead { get; set; }
}
