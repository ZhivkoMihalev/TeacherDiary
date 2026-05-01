using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TeacherDiary.Application.Abstractions.Auth;
using TeacherDiary.Application.Abstractions.Persistence;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Events;
using TeacherDiary.Infrastructure.Auth;
using TeacherDiary.Infrastructure.BackgroundServices;
using TeacherDiary.Infrastructure.Events;
using TeacherDiary.Infrastructure.Handlers;
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
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IStudentSelfService, StudentSelfService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Domain events
        services.AddScoped<IEventDispatcher, EventDispatcher>();

        // Notification handlers
        services.AddScoped<IDomainEventHandler<AssignmentCreatedEvent>, AssignmentCreatedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<AssignmentCompletedEvent>, AssignmentCompletedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<AssignmentOverdueEvent>, AssignmentOverdueNotificationHandler>();
        services.AddScoped<IDomainEventHandler<BookAssignedEvent>, BookAssignedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<BookCompletedEvent>, BookCompletedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<BookOverdueEvent>, BookOverdueNotificationHandler>();
        services.AddScoped<IDomainEventHandler<ChallengeCreatedEvent>, ChallengeCreatedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<ChallengeCompletedEvent>, ChallengeCompletedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<BadgeEarnedEvent>, BadgeEarnedNotificationHandler>();
        services.AddScoped<IDomainEventHandler<StreakBrokenEvent>, StreakBrokenNotificationHandler>();
        services.AddScoped<IDomainEventHandler<StreakReminderEvent>, StreakReminderNotificationHandler>();
        services.AddScoped<IDomainEventHandler<StudentJoinedClassEvent>, StudentJoinedClassNotificationHandler>();

        // Background services
        services.AddHostedService<OverdueAndReminderService>();

        return services;
    }
}   

