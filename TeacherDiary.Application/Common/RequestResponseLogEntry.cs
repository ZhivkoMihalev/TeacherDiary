namespace TeacherDiary.Application.Common;

public sealed class RequestResponseLogEntry
{
    public DateTime TimestampUtc { get; set; }

    public string TraceId { get; set; } = default!;

    public string Method { get; set; } = default!;

    public string Scheme { get; set; } = default!;

    public string Host { get; set; } = default!;

    public string Path { get; set; } = default!;

    public string QueryString { get; set; } = default!;

    public string? RequestContentType { get; set; }

    public string RequestBody { get; set; } = default!;

    public int ResponseStatusCode { get; set; }

    public string? ResponseContentType { get; set; }

    public string ResponseBody { get; set; } = default!;

    public long ElapsedMilliseconds { get; set; }
}