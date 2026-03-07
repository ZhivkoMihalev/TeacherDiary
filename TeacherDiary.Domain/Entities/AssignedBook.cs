namespace TeacherDiary.Domain.Entities;

public class AssignedBook : BaseEntity
{
    public Guid ClassId { get; set; }

    public Class Class { get; set; } = default!;

    public Guid BookId { get; set; }

    public Book Book { get; set; } = default!;

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public ICollection<ReadingProgress> ReadingProgress { get; set; } = new List<ReadingProgress>();
}
