using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class StudentService(AppDbContext db, ICurrentUser currentUser, IEventDispatcher eventDispatcher) : IStudentService
{
    public async Task<Result<List<StudentDto>>> GetByClassAsync(Guid classId, CancellationToken cancellationToken)
    {
        var ok = await db.Classes.AnyAsync(
            c => c.Id == classId
                 && c.OrganizationId == currentUser.OrganizationId
                 && c.TeacherId == currentUser.UserId,
            cancellationToken);

        if (!ok) return Result<List<StudentDto>>.Fail($"Class with id: {classId} was not found.");

        var list = await db.Students
            .AsNoTracking()
            .Where(s => s.ClassId == classId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                ClassId = s.ClassId,
                FirstName = s.FirstName,
                LastName = s.LastName,
                IsActive = s.IsActive
            })
            .ToListAsync(cancellationToken);

        var ids = list.Select(s => s.Id).ToList();
        var streaks = await db.StudentStreaks
            .AsNoTracking()
            .Where(s => ids.Contains(s.StudentProfileId))
            .Select(s => new { s.StudentProfileId, s.BestStreak })
            .ToListAsync(cancellationToken);

        var points = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => ids.Contains(a.StudentProfileId))
            .GroupBy(a => a.StudentProfileId)
            .Select(g => new { StudentProfileId = g.Key, TotalPoints = g.Sum(a => a.PointsEarned ?? 0) })
            .ToListAsync(cancellationToken);

        var streakMap = streaks.ToDictionary(s => s.StudentProfileId, s => s.BestStreak);
        var pointsMap = points.ToDictionary(p => p.StudentProfileId, p => p.TotalPoints);
        foreach (var s in list)
        {
            if (streakMap.TryGetValue(s.Id, out var best))
                s.TopMedalCode = BadgeCodes.GetStreakMedalCode(best);
            if (pointsMap.TryGetValue(s.Id, out var pts))
                s.TopPointsMedalCode = BadgeCodes.GetPointsMedalCode(pts);
        }

        return Result<List<StudentDto>>.Ok(list);
    }

    public async Task<Result<bool>> AddStudentToClassAsync(Guid classId, Guid studentId, CancellationToken cancellationToken)
    {
        var cls = await db.Classes
            .FirstOrDefaultAsync(c =>
                    c.Id == classId &&
                    c.TeacherId == currentUser.UserId &&
                    c.OrganizationId == currentUser.OrganizationId,
                cancellationToken);

        if (cls is null)
            return Result<bool>.Fail("Class not found.");

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
            return Result<bool>.Fail("Student not found.");

        student.ClassId = cls.Id;

        var existingReadingBookIds = await db.ReadingProgress
            .Where(r => r.StudentProfileId == student.Id)
            .Select(r => r.AssignedBookId)
            .ToListAsync(cancellationToken);

        var existingAssignmentIds = await db.AssignmentProgress
            .Where(a => a.StudentProfileId == student.Id)
            .Select(a => a.AssignmentId)
            .ToListAsync(cancellationToken);

        var existingChallengeIds = await db.ChallengeProgress
            .Where(c => c.StudentProfileId == student.Id)
            .Select(c => c.ChallengeId)
            .ToListAsync(cancellationToken);

        var existingActivityIds = await db.StudentLearningActivityProgress
            .Where(p => p.StudentProfileId == student.Id)
            .Select(p => p.LearningActivityId)
            .ToListAsync(cancellationToken);

        var assignedBooks = await db.AssignedBooks
            .Where(b => b.ClassId == cls.Id && !existingReadingBookIds.Contains(b.Id))
            .Select(b => new { b.Id, TotalPages = b.Book.TotalPages })
            .ToListAsync(cancellationToken);

        var readingRows = assignedBooks.Select(book => new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId = book.Id,
            Status = ProgressStatus.NotStarted,
            CurrentPage = 0,
            TotalPages = book.TotalPages
        });

        db.ReadingProgress.AddRange(readingRows);

        var assignments = await db.Assignments
            .Where(a => a.ClassId == cls.Id && !existingAssignmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var assignmentRows = assignments.Select(a => new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId = a.Id,
            Status = ProgressStatus.NotStarted
        });

        db.AssignmentProgress.AddRange(assignmentRows);

        var challenges = await db.Challenges
            .Where(c => c.ClassId == cls.Id && !existingChallengeIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var challengeRows = challenges.Select(c => new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = c.Id,
            CurrentValue = 0
        });

        db.ChallengeProgress.AddRange(challengeRows);

        var activities = await db.LearningActivities
            .Where(a => a.ClassId == cls.Id && a.IsActive && !existingActivityIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        var activityRows = activities.Select(a => new StudentLearningActivityProgress
        {
            StudentProfileId = student.Id,
            LearningActivityId = a.Id,
            Status = ProgressStatus.NotStarted,
            CurrentValue = 0,
            TargetValue = a.TargetValue
        });

        db.StudentLearningActivityProgress.AddRange(activityRows);

        await db.SaveChangesAsync(cancellationToken);

        await eventDispatcher.PublishAsync(
            new StudentJoinedClassEvent(student.Id, cls.Id),
            cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<PagedResult<StudentSearchDto>>> SearchAsync(
        string name,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        name = name.Trim().ToLower();

        var query = db.Students
            .AsNoTracking()
            .Where(s =>
                (s.ClassId == null ||
                 db.Classes.Any(c => c.Id == s.ClassId && c.OrganizationId == currentUser.OrganizationId)) &&
                (s.FirstName.ToLower().Contains(name) ||
                 s.LastName.ToLower().Contains(name)));

        var total = await query.CountAsync(cancellationToken);

        var students = await query
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StudentSearchDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                ClassId = s.ClassId
            })
            .ToListAsync(cancellationToken);

        return Result<PagedResult<StudentSearchDto>>.Ok(new PagedResult<StudentSearchDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = students
        });
    }

    public async Task<Result<bool>> RemoveStudentFromClassAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
            return Result<bool>.Fail($"Student with id {studentId} was not found.");

        if (student.ClassId is null)
            return Result<bool>.Fail("Student is not assigned to a class.");

        var currentClass = await db.Classes
            .FirstOrDefaultAsync(c =>
                    c.Id == student.ClassId &&
                    c.TeacherId == currentUser.UserId &&
                    c.OrganizationId == currentUser.OrganizationId,
                cancellationToken);

        if (currentClass is null)
            return Result<bool>.Fail("Forbidden.");

        student.ClassId = null;

        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}