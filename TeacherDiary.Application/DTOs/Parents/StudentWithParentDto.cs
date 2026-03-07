namespace TeacherDiary.Application.DTOs.Parents;

public class StudentWithParentDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string ParentEmail { get; set; }

    public string ParentFullName { get; set; }
}
