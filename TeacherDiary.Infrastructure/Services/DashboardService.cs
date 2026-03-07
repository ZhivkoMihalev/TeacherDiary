using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Dashboard;
using TeacherDiary.Application.DTOs.Leaderboard;
using TeacherDiary.Application.DTOs.Students;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class DashboardService(AppDbContext db, ICurrentUser currentUser) : IDashboardService
{
    public async Task<Result<DashboardDto>> GetClassDashboardAsync(Guid classId, CancellationToken cancellationToken)
    {
        var currentClass = await db.Classes
            .AsNoTracking()
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c =>
                    c.Id == classId &&
                    c.OrganizationId == currentUser.OrganizationId &&
                    c.TeacherId == currentUser.UserId,
                cancellationToken);

        if (currentClass is null)
            return Result<DashboardDto>.Fail($"Class with id: {classId} was not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6));

        var activeStudents = currentClass.Students
            .Where(s => s.IsActive)
            .ToList();

        var studentIds = activeStudents
            .Select(s => s.Id)
            .ToList();

        var studentsCount = activeStudents.Count;

        // Active today
        var activeToday = await db.ActivityLogs
            .Where(a => studentIds.Contains(a.StudentProfileId) && a.Date == today)
            .Select(a => a.StudentProfileId)
            .Distinct()
            .CountAsync(cancellationToken);

        // Pages read last 7 days
        var pagesLast7 = await db.ActivityLogs
            .Where(a =>
                studentIds.Contains(a.StudentProfileId) &&
                a.ActivityType == ActivityType.ReadingProgress &&
                a.Date >= from &&
                a.Date <= today)
            .SumAsync(a => a.PagesRead ?? 0, cancellationToken);

        // Completed assignments last 7 days
        var completedAssignments = await db.ActivityLogs
            .Where(a =>
                studentIds.Contains(a.StudentProfileId) &&
                a.ActivityType == ActivityType.AssignmentCompleted &&
                a.Date >= from &&
                a.Date <= today)
            .CountAsync(cancellationToken);

        var activeLearningActivitiesCount = await db.LearningActivities
            .CountAsync(a =>
                    a.ClassId == classId &&
                    a.IsActive,
                cancellationToken);

        var completedLearningActivitiesLast7Days =
            await db.StudentLearningActivityProgress
                .Where(p =>
                    studentIds.Contains(p.StudentProfileId) &&
                    p.Status == ProgressStatus.Completed &&
                    p.CompletedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync(cancellationToken);

        // Leaderboard by points
        var leaderboard = await db.StudentPoints
            .Where(p => studentIds.Contains(p.StudentProfileId))
            .OrderByDescending(p => p.TotalPoints)
            .Take(5)
            .Select(p => new LeaderboardItemDto
            {
                StudentId = p.StudentProfileId,
                StudentName = p.StudentProfile.FirstName + " " + p.StudentProfile.LastName,
                Points = p.TotalPoints
            })
            .ToListAsync(cancellationToken);

        // Top readers by pages last 7 days
        var topReadersRaw = await db.ActivityLogs
            .Where(a =>
                studentIds.Contains(a.StudentProfileId) &&
                a.ActivityType == ActivityType.ReadingProgress &&
                a.Date >= from &&
                a.Date <= today)
            .GroupBy(a => a.StudentProfileId)
            .Select(g => new
            {
                StudentId = g.Key,
                PagesRead = g.Sum(x => x.PagesRead ?? 0)
            })
            .OrderByDescending(x => x.PagesRead)
            .Take(5)
            .ToListAsync(cancellationToken);

        var topReaders = topReadersRaw
            .Join(activeStudents,
                tr => tr.StudentId,
                s => s.Id,
                (tr, s) => new TopReaderDto
                {
                    StudentId = s.Id,
                    StudentName = $"{s.FirstName} {s.LastName}",
                    PagesReadLast7Days = tr.PagesRead
                })
            .ToList();

        // Best streaks
        var bestStreaks = await db.StudentStreaks
            .Where(s => studentIds.Contains(s.StudentProfileId))
            .OrderByDescending(s => s.BestStreak)
            .ThenByDescending(s => s.CurrentStreak)
            .Take(5)
            .Select(s => new StudentStreakDto
            {
                StudentId = s.StudentProfileId,
                StudentName = s.StudentProfile.FirstName + " " + s.StudentProfile.LastName,
                CurrentStreak = s.CurrentStreak,
                BestStreak = s.BestStreak
            })
            .ToListAsync(cancellationToken);

        // Recent badges (last 7 days)
        var recentBadges = await db.StudentBadges
            .Where(sb =>
                studentIds.Contains(sb.StudentProfileId) &&
                sb.AwardedAt >= DateTime.UtcNow.AddDays(-7))
            .OrderByDescending(sb => sb.AwardedAt)
            .Take(10)
            .Select(sb => new RecentBadgeDto
            {
                StudentId = sb.StudentProfileId,
                StudentName = sb.StudentProfile.FirstName + " " + sb.StudentProfile.LastName,
                BadgeCode = sb.Badge.Code,
                BadgeName = sb.Badge.Name,
                BadgeIcon = sb.Badge.Icon,
                AwardedAt = sb.AwardedAt
            })
            .ToListAsync(cancellationToken);

        return Result<DashboardDto>.Ok(new DashboardDto
        {
            ClassId = currentClass.Id,
            ClassName = currentClass.Name,
            StudentsCount = studentsCount,
            ActiveTodayCount = activeToday,
            InactiveTodayCount = Math.Max(0, studentsCount - activeToday),
            TotalPagesReadLast7Days = pagesLast7,
            CompletedAssignmentsLast7Days = completedAssignments,
            Leaderboard = leaderboard,
            TopReaders = topReaders,
            BestStreaks = bestStreaks,
            RecentBadges = recentBadges,
            ActiveLearningActivitiesCount = activeLearningActivitiesCount,
            CompletedLearningActivitiesLast7Days = completedLearningActivitiesLast7Days
        });
    }

    public async Task<Result<List<StudentActivityDto>>> GetClassStudentActivityAsync(Guid classId, CancellationToken cancellationToken)
    {
        var currentClass = await db.Classes
            .AsNoTracking()
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c =>
                    c.Id == classId &&
                    c.OrganizationId == currentUser.OrganizationId &&
                    c.TeacherId == currentUser.UserId,
                cancellationToken);

        if (currentClass is null)
            return Result<List<StudentActivityDto>>.Fail($"Class with id {classId} was not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var students = currentClass.Students
            .Where(s => s.IsActive)
            .ToList();

        var studentIds = students.Select(s => s.Id).ToList();

        var todayLogs = await db.ActivityLogs
            .Where(a =>
                studentIds.Contains(a.StudentProfileId) &&
                a.Date == today)
            .ToListAsync(cancellationToken);

        var lastActivity = await db.ActivityLogs
            .Where(a => studentIds.Contains(a.StudentProfileId))
            .GroupBy(a => a.StudentProfileId)
            .Select(g => new
            {
                StudentId = g.Key,
                LastActivity = g.Max(x => x.CreatedAt)
            })
            .ToListAsync(cancellationToken);

        var result = students.Select(student =>
        {
            var logs = todayLogs
                .Where(a => a.StudentProfileId == student.Id)
                .ToList();

            var pagesRead = logs
                .Where(a => a.ActivityType == ActivityType.ReadingProgress)
                .Sum(a => a.PagesRead ?? 0);

            var assignmentsCompleted = logs
                .Count(a => a.ActivityType == ActivityType.AssignmentCompleted);

            var last = lastActivity
                .FirstOrDefault(x => x.StudentId == student.Id);

            return new StudentActivityDto
            {
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                PagesReadToday = pagesRead,
                AssignmentsCompletedToday = assignmentsCompleted,
                LastActivityAt = last?.LastActivity,
                IsActiveToday = logs.Any()
            };
        })
        .OrderByDescending(x => x.PagesReadToday)
        .ToList();

        return Result<List<StudentActivityDto>>.Ok(result);
    }

    public async Task<Result<StudentDetailsDto>> GetStudentDetailsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
            return Result<StudentDetailsDto>.Fail($"Student with id {studentId} was not found.");

        // security check
        var classExists = await db.Classes.AnyAsync(c =>
            c.Id == student.ClassId &&
            c.TeacherId == currentUser.UserId &&
            c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<StudentDetailsDto>.Fail("Forbidden.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-6);

        // reading progress
        var reading = await db.ReadingProgress
            .Where(r => r.StudentProfileId == studentId)
            .Select(r => new StudentReadingDto
            {
                BookTitle = r.AssignedBook.Book.Title,
                CurrentPage = r.CurrentPage,
                TotalPages = r.TotalPages,
                Status = r.Status
            })
            .ToListAsync(cancellationToken);

        // assignments
        var assignments = await db.AssignmentProgress
            .Where(a => a.StudentProfileId == studentId)
            .Select(a => new StudentAssignmentDto
            {
                Title = a.Assignment.Title,
                Subject = a.Assignment.Subject,
                Status = a.Status,
                DueDate = a.Assignment.DueDate
            })
            .ToListAsync(cancellationToken);

        // activity last 7 days
        var activityLast7 = await db.ActivityLogs
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
                    .Count(a => a.ActivityType == ActivityType.AssignmentCompleted)
            })
            .OrderBy(x => x.Date)
            .ToList();

        // last activity
        var lastActivity = await db.ActivityLogs
            .Where(a => a.StudentProfileId == studentId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // statistics
        var stats = await db.ActivityLogs
            .Where(a => a.StudentProfileId == studentId)
            .GroupBy(a => 1)
            .Select(g => new
            {
                PagesRead = g
                    .Where(a => a.ActivityType == ActivityType.ReadingProgress)
                    .Sum(a => a.PagesRead ?? 0),

                AssignmentsCompleted = g
                    .Count(a => a.ActivityType == ActivityType.AssignmentCompleted)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return Result<StudentDetailsDto>.Ok(new StudentDetailsDto
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            IsActive = student.IsActive,
            LastActivityAt = lastActivity,
            TotalPagesRead = stats?.PagesRead ?? 0,
            CompletedAssignments = stats?.AssignmentsCompleted ?? 0,
            Reading = reading,
            Assignments = assignments,
            ActivityLast7Days = activityByDay
        });
    }

    public async Task<Result<List<StudentBadgeDto>>> GetStudentBadgesAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var student = await db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
            return Result<List<StudentBadgeDto>>.Fail($"Student with id {studentId} was not found.");

        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == student.ClassId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<StudentBadgeDto>>.Fail("Forbidden.");

        var badges = await db.StudentBadges
            .Where(sb => sb.StudentProfileId == studentId)
            .OrderByDescending(sb => sb.AwardedAt)
            .Select(sb => new StudentBadgeDto
            {
                Code = sb.Badge.Code,
                Name = sb.Badge.Name,
                Description = sb.Badge.Description,
                Icon = sb.Badge.Icon,
                AwardedAt = sb.AwardedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<StudentBadgeDto>>.Ok(badges);
    }
}
