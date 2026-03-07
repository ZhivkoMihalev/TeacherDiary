namespace TeacherDiary.Application.DTOs.Students;

public sealed class CreateStudentRequest
{
    public string FirstName { get; set; } = default!;

    public string LastName { get; set; } = default!;
}
