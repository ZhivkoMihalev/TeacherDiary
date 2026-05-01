using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Extensions;

public static class BadgeSeeder
{
    private record BadgeDef(string Code, string Name, string Description, string Icon);

    private static readonly BadgeDef[] Definitions =
    [
        new(BadgeCodes.FirstBookCompleted, "Първа книга",     "Завърши първата си книга.",     "book"),
        new(BadgeCodes.Read100Pages,       "100 страници",    "Прочете общо 100 страници.",    "pages"),
        new(BadgeCodes.Complete5Assignments,"Упорит ученик",  "Завърши 5 задачи.",             "assignment"),
        new(BadgeCodes.Reach100Points,     "Постижение 100т", "Събра първите 100 точки.",      "star"),

        // Streak medals
        new(BadgeCodes.Streak3,       "3 дни подред",   "Беше активен 3 дни подред.",   "🌱"),
        new(BadgeCodes.Streak5,       "5 дни подред",   "Беше активен 5 дни подред.",   "⚡"),
        new(BadgeCodes.SevenDayStreak,"7 дни подред",   "Беше активен 7 дни подред.",   "🔥"),
        new(BadgeCodes.Streak15,      "15 дни подред",  "Беше активен 15 дни подред.",  "🌟"),
        new(BadgeCodes.Streak30,      "30 дни подред",  "Беше активен 30 дни подред.",  "🥉"),
        new(BadgeCodes.Streak45,      "45 дни подред",  "Беше активен 45 дни подред.",  "🥈"),
        new(BadgeCodes.Streak60,      "60 дни подред",  "Беше активен 60 дни подред.",  "🥇"),
        new(BadgeCodes.Streak90,      "90 дни подред",  "Беше активен 90 дни подред.",  "💎"),
        new(BadgeCodes.Streak180,     "180 дни подред", "Беше активен 180 дни подред.", "👑"),
        new(BadgeCodes.Streak360,     "360 дни подред", "Беше активен 360 дни подред.", "🏆"),

        // Points medals
        new(BadgeCodes.Points100,   "100 точки",    "Събра общо 100 точки от четене, задачи и предизвикателства.",    "🎯"),
        new(BadgeCodes.Points250,   "250 точки",    "Събра общо 250 точки от четене, задачи и предизвикателства.",    "🌈"),
        new(BadgeCodes.Points500,   "500 точки",    "Събра общо 500 точки от четене, задачи и предизвикателства.",    "🚀"),
        new(BadgeCodes.Points1000,  "1000 точки",   "Събра общо 1000 точки от четене, задачи и предизвикателства.",   "💫"),
        new(BadgeCodes.Points1500,  "1500 точки",   "Събра общо 1500 точки от четене, задачи и предизвикателства.",   "🦋"),
        new(BadgeCodes.Points2000,  "2000 точки",   "Събра общо 2000 точки от четене, задачи и предизвикателства.",   "🔮"),
        new(BadgeCodes.Points3000,  "3000 точки",   "Събра общо 3000 точки от четене, задачи и предизвикателства.",   "🌀"),
        new(BadgeCodes.Points5000,  "5000 точки",   "Събра общо 5000 точки от четене, задачи и предизвикателства.",   "⚜️"),
        new(BadgeCodes.Points7500,  "7500 точки",   "Събра общо 7500 точки от четене, задачи и предизвикателства.",   "🦅"),
        new(BadgeCodes.Points10000, "10 000 точки", "Събра общо 10 000 точки от четене, задачи и предизвикателства.", "🏛️"),
    ];

    public static async Task SeedBadgesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await db.Badges.ToListAsync();
        var existingByCode = existing.ToDictionary(b => b.Code, StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Badge>();

        foreach (var def in Definitions)
        {
            if (existingByCode.TryGetValue(def.Code, out var badge))
            {
                if (badge.Icon != def.Icon)             badge.Icon = def.Icon;
                if (badge.Name != def.Name)             badge.Name = def.Name;
                if (badge.Description != def.Description) badge.Description = def.Description;
            }
            else
            {
                toAdd.Add(new Badge
                {
                    Code        = def.Code,
                    Name        = def.Name,
                    Description = def.Description,
                    Icon        = def.Icon
                });
            }
        }

        // Commit updates to existing badges first so renamed Names are freed
        // before inserting new badges that may reuse those Names.
        await db.SaveChangesAsync();

        if (toAdd.Count > 0)
        {
            db.Badges.AddRange(toAdd);
            await db.SaveChangesAsync();
        }
    }
}
