namespace TeacherDiary.Api.Middlewares;

public static class MiddlewaresExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
