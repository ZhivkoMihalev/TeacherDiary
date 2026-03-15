namespace TeacherDiary.Application.Common;

public sealed class ErrorResponse
{
    public string Message { get; set; } = default!;

    public int StatusCode { get; set; }

    public string TraceId { get; set; } = default!;

    public DateTime Timestamp { get; set; }
}
