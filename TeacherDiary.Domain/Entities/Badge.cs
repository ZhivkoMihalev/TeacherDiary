namespace TeacherDiary.Domain.Entities;

public class Badge : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Code { get; set; } = default!;

    public string Description { get; set; } = default!;

    public string Icon { get; set; } = default!;

    public ICollection<StudentBadge> StudentBadges { get; set; } = new List<StudentBadge>();
}
