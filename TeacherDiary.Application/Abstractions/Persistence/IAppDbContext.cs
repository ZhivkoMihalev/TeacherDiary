using Microsoft.EntityFrameworkCore;
using TeacherDiary.Domain.Entities;

namespace TeacherDiary.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }

    DbSet<Class> Classes { get; }

    DbSet<StudentProfile> Students { get; }

    DbSet<Book> Books { get; }

    DbSet<AssignedBook> AssignedBooks { get; }

    DbSet<ReadingProgress> ReadingProgress { get; }

    DbSet<Assignment> Assignments { get; }

    DbSet<AssignmentProgress> AssignmentProgress { get; }

    DbSet<Challenge> Challenges { get; }

    DbSet<ChallengeProgress> ChallengeProgress { get; }

    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
