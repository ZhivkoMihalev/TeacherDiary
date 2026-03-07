namespace TeacherDiary.Domain.Entities;

public class Assignment : BaseEntity
{
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = default!;

    public Guid CreatedByTeacherId { get; set; }

    public string Title { get; set; } = default!;

    public string Description { get; set; }

    public string Subject { get; set; } = "General";

    public DateTime? DueDate { get; set; }

    public ICollection<AssignmentProgress> Progress { get; set; } = new List<AssignmentProgress>();
}
