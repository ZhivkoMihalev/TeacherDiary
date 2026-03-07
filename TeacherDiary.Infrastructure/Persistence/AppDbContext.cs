using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Persistence;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Auth;

namespace TeacherDiary.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser, AppRole, Guid>(options), IAppDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Class> Classes => Set<Class>();

    public DbSet<StudentProfile> Students => Set<StudentProfile>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<AssignedBook> AssignedBooks => Set<AssignedBook>();

    public DbSet<ReadingProgress> ReadingProgress => Set<ReadingProgress>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<AssignmentProgress> AssignmentProgress => Set<AssignmentProgress>();

    public DbSet<Challenge> Challenges => Set<Challenge>();

    public DbSet<ChallengeProgress> ChallengeProgress => Set<ChallengeProgress>();

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    public DbSet<StudentPoints> StudentPoints => Set<StudentPoints>();

    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();

    public DbSet<StudentStreak> StudentStreaks => Set<StudentStreak>();

    public DbSet<Badge> Badges => Set<Badge>();

    public DbSet<LearningActivity> LearningActivities => Set<LearningActivity>();

    public DbSet<StudentLearningActivityProgress> StudentLearningActivityProgress => Set<StudentLearningActivityProgress>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        foreach (var relationship in b.Model
                     .GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // Organization relation
        b.Entity<Organization>().HasIndex(x => x.Name);

        // Class relation
        b.Entity<Class>()
            .HasOne(x => x.Organization)
            .WithMany(o => o.Classes)
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<Class>()
            .HasIndex(x => new { x.TeacherId, x.OrganizationId });

        // StudentProfile relation
        b.Entity<StudentProfile>()
            .HasIndex(s => s.ClassId);

        b.Entity<StudentProfile>()
            .HasIndex(s => new { s.FirstName, s.LastName });

        // AssignedBook relation
        b.Entity<AssignedBook>()
            .HasOne(x => x.Class)
            .WithMany(c => c.AssignedBooks)
            .HasForeignKey(x => x.ClassId);

        b.Entity<AssignedBook>()
            .HasOne(x => x.Book)
            .WithMany()
            .HasForeignKey(x => x.BookId);

        // ReadingProgress relation
        b.Entity<ReadingProgress>()
            .HasIndex(x => new { x.StudentProfileId, x.AssignedBookId })
            .IsUnique();

        // AssignmentProgress relation
        b.Entity<AssignmentProgress>()
            .HasIndex(x => new { x.StudentProfileId, x.AssignmentId })
            .IsUnique();

        // ActivityLog relation
        b.Entity<ActivityLog>()
            .HasIndex(x => new { x.StudentProfileId, x.Date });

        b.Entity<ActivityLog>()
            .HasIndex(x => new { x.StudentProfileId, x.ActivityType });

        // StudentProfile relation
        b.Entity<StudentProfile>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<StudentProfile>()
            .HasIndex(x => x.ClassId);

        // Points relation
        b.Entity<StudentPoints>()
            .HasOne(p => p.StudentProfile)
            .WithOne(s => s.Points)
            .HasForeignKey<StudentPoints>(p => p.StudentProfileId);

        b.Entity<StudentPoints>()
            .HasIndex(x => x.StudentProfileId);

        // Streak relation
        b.Entity<StudentStreak>()
            .HasOne(s => s.StudentProfile)
            .WithOne(p => p.Streak)
            .HasForeignKey<StudentStreak>(s => s.StudentProfileId);

        // StudentBadge relation
        b.Entity<StudentBadge>()
            .HasOne(sb => sb.StudentProfile)
            .WithMany(s => s.Badges)
            .HasForeignKey(sb => sb.StudentProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<StudentBadge>()
            .HasOne(sb => sb.Badge)
            .WithMany(b => b.StudentBadges)
            .HasForeignKey(sb => sb.BadgeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<StudentBadge>()
            .HasIndex(x => new { x.StudentProfileId, x.BadgeId })
            .IsUnique();

        // Badge relation
        b.Entity<Badge>(e =>
        {
            e.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            e.HasIndex(x => x.Code)
                .IsUnique();

            e.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            e.HasIndex(x => x.Name)
                .IsUnique();

            e.Property(x => x.Description)
                .HasMaxLength(500);

            e.Property(x => x.Icon)
                .HasMaxLength(200);
        });

        b.Entity<LearningActivity>()
            .HasOne(x => x.Class)
            .WithMany(c => c.LearningActivities)
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<LearningActivity>()
            .HasIndex(x => new { x.ClassId, x.Type });

        b.Entity<LearningActivity>()
            .HasIndex(x => x.AssignmentId);

        b.Entity<LearningActivity>()
            .HasIndex(x => x.AssignedBookId);

        b.Entity<LearningActivity>()
            .HasIndex(x => x.ChallengeId);

        b.Entity<StudentLearningActivityProgress>()
            .HasOne(x => x.LearningActivity)
            .WithMany(a => a.StudentProgress)
            .HasForeignKey(x => x.LearningActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<StudentLearningActivityProgress>()
            .HasOne(x => x.StudentProfile)
            .WithMany(s => s.LearningActivityProgress)
            .HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<StudentLearningActivityProgress>()
            .HasIndex(x => new { x.StudentProfileId, x.LearningActivityId })
            .IsUnique();

        b.Entity<StudentLearningActivityProgress>()
            .HasIndex(x => new { x.StudentProfileId, x.Status });
    }
}
