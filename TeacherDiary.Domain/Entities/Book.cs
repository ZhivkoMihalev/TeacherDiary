namespace TeacherDiary.Domain.Entities;

public class Book : BaseEntity
{
    public string Title { get; set; } = default!;

    public string Author { get; set; }

    public string Description { get; set; }

    public int? GradeLevel { get; set; }

    public Guid? CreatedByTeacherId { get; set; }

    public bool IsGlobal { get; set; } = false;

    public int? TotalPages { get; set; }
}
