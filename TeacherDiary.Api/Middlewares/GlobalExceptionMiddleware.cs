using System.Text.Json;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.Exceptions;

namespace TeacherDiary.Api.Middlewares;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception. Method: {Method}, Path: {Path}, TraceId: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var response = new ErrorResponse
        {
            Message = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message,
            StatusCode = statusCode,
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}
