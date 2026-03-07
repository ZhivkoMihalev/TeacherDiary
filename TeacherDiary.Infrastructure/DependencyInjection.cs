using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeacherDiary.Application.Abstractions.Auth;
using TeacherDiary.Application.Abstractions.Persistence;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.Persistence;
using TeacherDiary.Infrastructure.Services;

namespace TeacherDiary.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(cfg.GetConnectionString("DefaultConnection")));
    
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services
            .AddIdentityCore<AppUser>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireDigit = true;
                opt.Password.RequireUppercase = false;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireNonAlphanumeric = false;
                opt.User.RequireUniqueEmail = true;
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>();
            //.AddDefaultTokenProviders();
    
        services.Configure<JwtOptions>(cfg.GetSection("Jwt"));
    
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
    
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IReadingService, ReadingService>();
        services.AddScoped<IAssignmentService, AssignmentService>();
        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IParentService, ParentService>();
        services.AddScoped<IGamificationService, GamificationService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IBadgeService, BadgeService>();
        services.AddScoped<ILearningActivityService, LearningActivityService>();

        return services;
    }
}   
