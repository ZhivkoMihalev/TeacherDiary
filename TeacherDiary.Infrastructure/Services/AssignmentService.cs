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
    ILearningActivityService learningActivityService) : IAssignmentService
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
            DueDate = request.DueDate
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
            .FirstOrDefaultAsync(p =>
                    p.StudentProfileId == studentId &&
                    p.AssignmentId == assignmentId,
                cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Progress not found.");

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
                DueDate = a.DueDate,
                TotalStudents = a.Progress.Count(),
                CompletedStudents = a.Progress.Count(p => p.Status == ProgressStatus.Completed)
            })
            .OrderByDescending(a => a.DueDate)
            .ToListAsync(cancellationToken);

        return Result<List<AssignmentListDto>>.Ok(assignments);
    }
}
