namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentSearchDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public Guid? ClassId { get; set; }
}
