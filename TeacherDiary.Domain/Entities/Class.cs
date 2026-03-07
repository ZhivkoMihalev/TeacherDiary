namespace TeacherDiary.Domain.Entities;

public class Class : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; } = default!;

    /// <summary>
    /// For example: 3A / 5B or etc.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Total grade of the class /2-6/.
    /// </summary>
    public int Grade { get; set; }

    /// <summary>
    /// For example: 2000/2001
    /// </summary>
    public string SchoolYear { get; set; } = default!;

    public Guid TeacherId { get; set; }

    public ICollection<StudentProfile> Students { get; set; } = new List<StudentProfile>();

    public ICollection<AssignedBook> AssignedBooks { get; set; } = new List<AssignedBook>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

    public ICollection<Challenge> Challenges { get; set; } = new List<Challenge>();

    public ICollection<LearningActivity> LearningActivities { get; set; }
        = new List<LearningActivity>();
}