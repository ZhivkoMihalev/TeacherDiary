using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Extensions;

public static class BadgeSeeder
{
    public static async Task SeedBadgesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingCodes = await db.Badges
            .Select(b => b.Code)
            .ToListAsync();

        var existing = existingCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var badges = new List<Badge>();

        void AddIfMissing(string code, string name, string description, string icon)
        {
            if (!existing.Contains(code))
            {
                badges.Add(new Badge
                {
                    Code = code,
                    Name = name,
                    Description = description,
                    Icon = icon
                });
            }
        }

        AddIfMissing(
            BadgeCodes.FirstBookCompleted,
            "Първа книга",
            "Завърши първата си книга.",
            "book");

        AddIfMissing(
            BadgeCodes.Read100Pages,
            "100 страници",
            "Прочете общо 100 страници.",
            "pages");

        AddIfMissing(
            BadgeCodes.Complete5Assignments,
            "Упорит ученик",
            "Завърши 5 задачи.",
            "assignment");

        AddIfMissing(
            BadgeCodes.SevenDayStreak,
            "7 дни подред",
            "Беше активен 7 дни подред.",
            "streak");

        AddIfMissing(
            BadgeCodes.Reach100Points,
            "100 точки",
            "Събра 100 точки.",
            "star");

        if (badges.Count > 0)
        {
            db.Badges.AddRange(badges);
            await db.SaveChangesAsync();
        }
    }
}
