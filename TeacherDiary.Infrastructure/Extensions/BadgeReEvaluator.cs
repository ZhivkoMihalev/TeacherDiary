using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Extensions;

public static class BadgeReEvaluator
{
    public static async Task ReEvaluateAllAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var badgeService = scope.ServiceProvider.GetRequiredService<IBadgeService>();

        var studentIds = await db.Students
            .Select(s => s.Id)
            .ToListAsync();

        foreach (var studentId in studentIds)
            await badgeService.EvaluateAsync(studentId, CancellationToken.None);

        await db.SaveChangesAsync();
    }
}
