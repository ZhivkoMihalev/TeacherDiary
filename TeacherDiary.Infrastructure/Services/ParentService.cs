using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Domain.Common;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ParentService(AppDbContext db, ICurrentUser currentUser) : IParentService
{
    public async Task<Result<Guid>> CreateStudentAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
            return Result<Guid>.Fail("Unauthorized");

        var student = new StudentProfile
        {
            ParentId = currentUser.UserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ClassId = null
        };

        db.Students.Add(student);

        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(student.Id);
    }

    public async Task<Result<List<StudentDto>>> GetMyStudentsAsync(CancellationToken cancellationToken)
    {
        var students = await db.Students
            .AsNoTracking()
            .Where(s => s.ParentId == currentUser.UserId)
            .OrderBy(s => s.LastName)
            .ThenBy(s => s.FirstName)
            .Select(s => new StudentDto
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                ClassId = s.ClassId
            })
            .ToListAsync(cancellationToken);

        var parentIds = students.Select(s => s.Id).ToList();
        var parentStreaks = await db.StudentStreaks
            .AsNoTracking()
            .Where(s => parentIds.Contains(s.StudentProfileId))
            .Select(s => new { s.StudentProfileId, s.BestStreak })
            .ToListAsync(cancellationToken);

        var parentPoints = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => parentIds.Contains(a.StudentProfileId))
            .GroupBy(a => a.StudentProfileId)
            .Select(g => new { StudentProfileId = g.Key, TotalPoints = g.Sum(a => a.PointsEarned ?? 0) })
            .ToListAsync(cancellationToken);

        var parentStreakMap = parentStreaks.ToDictionary(s => s.StudentProfileId, s => s.BestStreak);
        var parentPointsMap = parentPoints.ToDictionary(p => p.StudentProfileId, p => p.TotalPoints);
        foreach (var s in students)
        {
            if (parentStreakMap.TryGetValue(s.Id, out var best))
                s.TopMedalCode = BadgeCodes.GetStreakMedalCode(best);
            if (parentPointsMap.TryGetValue(s.Id, out var pts))
                s.TopPointsMedalCode = BadgeCodes.GetPointsMedalCode(pts);
        }

        return Result<List<StudentDto>>.Ok(students);
    }

    public async Task<Result<StudentDetailsDto>> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                    s.Id == studentId &&
                    s.ParentId == currentUser.UserId,
                cancellationToken);

        if (student is null)
            return Result<StudentDetailsDto>.Fail($"Student with id {studentId} was not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-6);

        var reading = await db.ReadingProgress
            .AsNoTracking()
            .Where(r => r.StudentProfileId == studentId)
            .Select(r => new StudentReadingDto
            {
                AssignedBookId = r.AssignedBookId,
                BookTitle = r.AssignedBook.Book.Title,
                CurrentPage = r.CurrentPage,
                TotalPages = r.TotalPages,
                Status = r.Status,
                EndDateUtc = r.AssignedBook.EndDateUtc,
                IsExpired = r.AssignedBook.EndDateUtc.HasValue && r.AssignedBook.EndDateUtc.Value < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        var assignments = await db.AssignmentProgress
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId)
            .Select(a => new StudentAssignmentDto
            {
                AssignmentId = a.AssignmentId,
                Title = a.Assignment.Title,
                Subject = a.Assignment.Subject,
                Status = a.Status,
                DueDate = a.Assignment.DueDate,
                IsExpired = a.Assignment.DueDate.HasValue && a.Assignment.DueDate.Value < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        var activityLast7 = await db.ActivityLogs
            .AsNoTracking()
            .Where(a =>
                a.StudentProfileId == studentId &&
                a.Date >= from &&
                a.Date <= today)
            .ToListAsync(cancellationToken);

        var activityByDay = activityLast7
            .GroupBy(a => a.Date)
            .Select(g => new StudentActivityDayDto
            {
                Date = g.Key,
                PagesRead = g
                    .Where(a => a.ActivityType == ActivityType.ReadingProgress)
                    .Sum(a => a.PagesRead ?? 0),
                AssignmentsCompleted = g
                    .Count(a => a.ActivityType == ActivityType.AssignmentCompleted),
                PointsEarned = g.Sum(a => a.PointsEarned ?? 0)
            })
            .OrderBy(x => x.Date)
            .ToList();

        var lastActivity = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var stats = await db.ActivityLogs
            .AsNoTracking()
            .Where(a => a.StudentProfileId == studentId)
            .GroupBy(a => 1)
            .Select(g => new
            {
                PagesRead = g
                    .Where(a => a.ActivityType == ActivityType.ReadingProgress)
                    .Sum(a => a.PagesRead ?? 0),
                AssignmentsCompleted = g
                    .Count(a => a.ActivityType == ActivityType.AssignmentCompleted),
                TotalPoints = g.Sum(a => a.PointsEarned ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var learningActivities = await db.StudentLearningActivityProgress
            .AsNoTracking()
            .Where(p => p.StudentProfileId == studentId)
            .Select(p => new StudentLearningActivityDto
            {
                LearningActivityId = p.LearningActivityId,
                Title = p.LearningActivity.Title,
                Type = p.LearningActivity.Type,
                Status = p.Status,
                CurrentValue = p.CurrentValue,
                TargetValue = p.TargetValue,
                Score = p.Score,
                DueDateUtc = p.LearningActivity.DueDateUtc,
                IsExpired = p.LearningActivity.DueDateUtc.HasValue && p.LearningActivity.DueDateUtc.Value < DateTime.UtcNow
            })
            .ToListAsync(cancellationToken);

        var parentStudentBestStreak = await db.StudentStreaks
            .AsNoTracking()
            .Where(s => s.StudentProfileId == studentId)
            .Select(s => s.BestStreak)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<StudentDetailsDto>.Ok(new StudentDetailsDto
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            IsActive = student.IsActive,
            LastActivityAt = lastActivity,
            TotalPagesRead = stats?.PagesRead ?? 0,
            CompletedAssignments = stats?.AssignmentsCompleted ?? 0,
            TotalPoints = stats?.TotalPoints ?? 0,
            TopMedalCode = BadgeCodes.GetStreakMedalCode(parentStudentBestStreak),
            TopPointsMedalCode = BadgeCodes.GetPointsMedalCode(stats?.TotalPoints ?? 0),
            Reading = reading,
            Assignments = assignments,
            ActivityLast7Days = activityByDay,
            LearningActivities = learningActivities
        });
    }

    public async Task<Result<bool>> DeleteStudentAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s =>
                s.Id == studentId &&
                s.ParentId == currentUser.UserId,
                cancellationToken);

        if (student is null)
            return Result<bool>.Fail("Student not found.");

        if (student.ClassId is not null)
            return Result<bool>.Fail("Детето е записано в клас. Помолете учителя да го премахне от класа преди да го изтриете.");

        await db.ActivityLogs.Where(a => a.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.StudentPoints.Where(p => p.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.StudentStreaks.Where(s => s.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.StudentBadges.Where(sb => sb.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.StudentLearningActivityProgress.Where(p => p.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.ReadingProgress.Where(r => r.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.AssignmentProgress.Where(a => a.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);
        await db.ChallengeProgress.Where(c => c.StudentProfileId == studentId).ExecuteDeleteAsync(cancellationToken);

        db.Students.Remove(student);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}

