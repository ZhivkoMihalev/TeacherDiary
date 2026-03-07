namespace TeacherDiary.Application.DTOs.Parents;

public sealed class ParentStudentDto
{
    public Guid Id { get; set; }

    public string FirstName { get; set; }

    public string LastName { get; set; }

    public Guid? ClassId { get; set; }

    public bool IsActive { get; set; }
}
