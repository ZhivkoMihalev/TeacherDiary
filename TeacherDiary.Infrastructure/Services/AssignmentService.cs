using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Assignments;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class AssignmentService(
    AppDbContext db,
    ICurrentUser currentUser,
    IActivityService activityService,
    ILearningActivityService learningActivityService,
    IBadgeService badgeService) : IAssignmentService
{
    public async Task<Result<Guid>> CreateAssignmentAsync(
        Guid classId,
        AssignmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        var currentClass = await db.Classes.FirstOrDefaultAsync(
            c => c.Id == classId &&
                 c.OrganizationId == currentUser.OrganizationId &&
                 c.TeacherId == currentUser.UserId,
            cancellationToken);

        if (currentClass is null)
            return Result<Guid>.Fail($"Class with id: {classId} was not found.");

        var assignment = new Assignment
        {
            ClassId = currentClass.Id,
            CreatedByTeacherId = currentUser.UserId,
            Title = request.Title,
            Description = request.Description,
            Subject = request.Subject,
            DueDate = request.DueDate,
            Points = request.Points
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        var studentIds = await db.Students
            .Where(s => s.ClassId == currentClass.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var rows = studentIds.Select(studentId => new AssignmentProgress
        {
            StudentProfileId = studentId,
            AssignmentId = assignment.Id,
            Status = ProgressStatus.NotStarted
        });

        db.AssignmentProgress.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);

        await learningActivityService.CreateForAssignmentAsync(
            assignment,
            cancellationToken);

        return Result<Guid>.Ok(assignment.Id);
    }

    public async Task<Result<bool>> UpdateProgressAsync(
        Guid studentId,
        Guid assignmentId,
        bool completed,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
            return Result<bool>.Fail($"Student with id {studentId} was not found.");

        if (student.ParentId != currentUser.UserId)
            return Result<bool>.Fail("Forbidden.");

        var progress = await db.AssignmentProgress
            .Include(p => p.Assignment)
            .FirstOrDefaultAsync(p =>
                    p.StudentProfileId == studentId &&
                    p.AssignmentId == assignmentId,
                cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Progress not found.");

        if (progress.Assignment.DueDate.HasValue &&
            progress.Assignment.DueDate.Value < DateTime.UtcNow)
            return Result<bool>.Fail("Deadline has passed. Assignment progress is locked.");

        var wasCompleted = progress.Status == ProgressStatus.Completed;

        if (progress.StartedAt == null)
            progress.StartedAt = DateTime.UtcNow;

        progress.Status = completed
            ? ProgressStatus.Completed
            : ProgressStatus.InProgress;

        progress.LastUpdatedAt = DateTime.UtcNow;

        if (completed && !wasCompleted)
        {
            progress.CompletedAt = DateTime.UtcNow;

            await activityService.LogAssignmentCompletedAsync(
                studentId,
                assignmentId,
                progress.Assignment.Points,
                cancellationToken);
        }

        await learningActivityService.UpdateAssignmentProgressAsync(
            studentId,
            assignmentId,
            completed,
            progress.Score,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<AssignmentListDto>>> GetAssignmentsByClassAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<AssignmentListDto>>.Fail("Class not found.");

        var assignments = await db.Assignments
            .Where(a => a.ClassId == classId)
            .Select(a => new AssignmentListDto
            {
                Id = a.Id,
                Title = a.Title,
                Subject = a.Subject,
                Description = a.Description ?? string.Empty,
                DueDate = a.DueDate,
                Points = a.Points,
                TotalStudents = a.Progress.Count(p => p.StudentProfile.ClassId == classId),
                CompletedCount = a.Progress.Count(p => p.Status == ProgressStatus.Completed && p.StudentProfile.ClassId == classId),
                IsExpired = a.DueDate.HasValue && a.DueDate.Value < DateTime.UtcNow
            })
            .OrderByDescending(a => a.DueDate)
            .ToListAsync(cancellationToken);

        return Result<List<AssignmentListDto>>.Ok(assignments);
    }

    public async Task<Result<List<AssignmentStudentProgressDto>>> GetStudentProgressForAssignmentAsync(
        Guid classId,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<AssignmentStudentProgressDto>>.Fail("Class not found.");

        var progress = await db.AssignmentProgress
            .Where(p => p.AssignmentId == assignmentId && p.StudentProfile.ClassId == classId)
            .Select(p => new AssignmentStudentProgressDto
            {
                StudentId = p.StudentProfileId,
                StudentName = p.StudentProfile.FirstName + " " + p.StudentProfile.LastName,
                Status = p.Status
            })
            .OrderBy(p => p.StudentName)
            .ToListAsync(cancellationToken);

        return Result<List<AssignmentStudentProgressDto>>.Ok(progress);
    }

    public async Task<Result<bool>> UpdateAssignmentAsync(
        Guid classId,
        Guid assignmentId,
        AssignmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.FirstOrDefaultAsync(a =>
                a.Id == assignmentId &&
                a.ClassId == classId &&
                a.Class.TeacherId == currentUser.UserId,
            cancellationToken);

        if (assignment is null)
            return Result<bool>.Fail("Assignment not found.");

        int pointsDelta = request.Points - assignment.Points;

        assignment.Title = request.Title;
        assignment.Description = request.Description;
        assignment.Subject = request.Subject;
        assignment.DueDate = request.DueDate;
        assignment.Points = request.Points;

        if (pointsDelta != 0)
        {
            // Retroactively adjust every student who already completed this assignment
            var completedStudentIds = await db.AssignmentProgress
                .Where(p => p.AssignmentId == assignmentId && p.Status == ProgressStatus.Completed)
                .Select(p => p.StudentProfileId)
                .ToListAsync(cancellationToken);

            foreach (var studentId in completedStudentIds)
            {
                // Adjust StudentPoints (create record if it didn't exist yet)
                var sp = await db.StudentPoints
                    .FirstOrDefaultAsync(p => p.StudentProfileId == studentId, cancellationToken);

                if (sp is null && pointsDelta > 0)
                {
                    db.StudentPoints.Add(new StudentPoints
                    {
                        StudentProfileId = studentId,
                        TotalPoints = pointsDelta
                    });
                }
                else if (sp is not null)
                {
                    sp.TotalPoints = Math.Max(0, sp.TotalPoints + pointsDelta);
                    sp.LastUpdatedAt = DateTime.UtcNow;
                }

                // Keep the ActivityLog entry in sync with the new points value
                var log = await db.ActivityLogs
                    .Where(a =>
                        a.StudentProfileId == studentId &&
                        a.ActivityType == ActivityType.AssignmentCompleted &&
                        a.ReferenceId == assignmentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (log is not null)
                    log.PointsEarned = Math.Max(0, (log.PointsEarned ?? 0) + pointsDelta);

                await badgeService.EvaluateAsync(studentId, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }
}
