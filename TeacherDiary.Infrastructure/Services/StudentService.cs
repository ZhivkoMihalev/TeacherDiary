using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Domain.Common;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class StudentService(AppDbContext db, ICurrentUser currentUser) : IStudentService
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

        await db.SaveChangesAsync(cancellationToken);

        // Bootstrap reading progress
        var assignedBooks = await db.AssignedBooks
            .Where(b => b.ClassId == cls.Id)
            .ToListAsync(cancellationToken);

        var readingRows = assignedBooks.Select(book => new ReadingProgress
        {
            StudentProfileId = student.Id,
            AssignedBookId = book.Id,
            Status = ProgressStatus.NotStarted,
            CurrentPage = 0
        });

        db.ReadingProgress.AddRange(readingRows);

        // Bootstrap assignment progress
        var assignments = await db.Assignments
            .Where(a => a.ClassId == cls.Id)
            .ToListAsync(cancellationToken);

        var assignmentRows = assignments.Select(a => new AssignmentProgress
        {
            StudentProfileId = student.Id,
            AssignmentId = a.Id,
            Status = ProgressStatus.NotStarted
        });

        db.AssignmentProgress.AddRange(assignmentRows);

        // Bootstrap challenge progress
        var challenges = await db.Challenges
            .Where(c => c.ClassId == cls.Id)
            .ToListAsync(cancellationToken);

        var challengeRows = challenges.Select(c => new ChallengeProgress
        {
            StudentProfileId = student.Id,
            ChallengeId = c.Id,
            CurrentValue = 0
        });

        db.ChallengeProgress.AddRange(challengeRows);

        // Bootstrap learning activity engine
        var activities = await db.LearningActivities
            .Where(a => a.ClassId == cls.Id && a.IsActive)
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
                s.FirstName.ToLower().Contains(name) ||
                s.LastName.ToLower().Contains(name));

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
