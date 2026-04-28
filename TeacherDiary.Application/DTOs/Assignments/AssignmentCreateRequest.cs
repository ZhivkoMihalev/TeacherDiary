namespace TeacherDiary.Application.DTOs.Assignments;

public sealed class AssignmentCreateRequest
{
    /// <example>Домашна работа по математика</example>
    public string Title { get; set; } = default!;

    /// <example>Реши упражнения от 1 до 10</example>
    public string Description { get; set; }

    /// <example>Математика</example>
    public string Subject { get; set; }

    /// <example>2026-03-10</example>
    public DateTime? DueDate { get; set; }

    public int Points { get; set; }
}
