namespace TeacherDiary.Application.DTOs.Students;

public sealed class StudentDto
{
    public Guid Id { get; set; }

    public Guid? ClassId { get; set; }

    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public bool IsActive { get; set; }

    public string? TopMedalCode { get; set; }

    public string? TopPointsMedalCode { get; set; }
}
