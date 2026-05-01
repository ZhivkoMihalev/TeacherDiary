using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Challenges;
using TeacherDiary.Application.Events;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ChallengeService(
    AppDbContext db,
    ICurrentUser currentUser,
    ILearningActivityService learningActivityService,
    IEventDispatcher eventDispatcher) : IChallengeService
{
    public async Task<Result<Guid>> CreateChallengeAsync(
        Guid classId,
        ChallengeCreateRequest request,
        CancellationToken cancellationToken)
    {
        var currentClass = await db.Classes.FirstOrDefaultAsync(
            c => c.Id == classId &&
                 c.OrganizationId == currentUser.OrganizationId &&
                 c.TeacherId == currentUser.UserId,
            cancellationToken);

        if (currentClass is null)
            return Result<Guid>.Fail($"Class with id: {classId} was not found.");

        var challenge = new Challenge
        {
            ClassId = currentClass.Id,
            Title = request.Title,
            Description = request.Description,
            TargetDescription = request.TargetDescription,
            TargetType = request.TargetType,
            TargetValue = request.TargetValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Points = request.Points
        };

        db.Challenges.Add(challenge);
        await db.SaveChangesAsync(cancellationToken);

        var studentIds = await db.Students
            .Where(s => s.ClassId == currentClass.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var studentId in studentIds)
        {
            db.ChallengeProgress.Add(new ChallengeProgress
            {
                StudentProfileId = studentId,
                ChallengeId = challenge.Id,
                CurrentValue = 0
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        await learningActivityService.CreateForChallengeAsync(
            challenge,
            cancellationToken);

        await eventDispatcher.PublishAsync(
            new ChallengeCreatedEvent(challenge.Id, currentClass.Id, challenge.Title),
            cancellationToken);

        return Result<Guid>.Ok(challenge.Id);
    }

    public async Task<Result<List<ChallengeDto>>> GetChallengesAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<ChallengeDto>>.Fail($"Class with id {classId} was not found.");

        var challenges = await db.Challenges
            .Where(c => c.ClassId == classId)
            .Select(c => new ChallengeDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                TargetDescription = c.TargetDescription,
                TargetType = c.TargetType,
                TargetValue = c.TargetValue,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Points = c.Points,
                TotalStudents = c.Progress.Count(p => p.StudentProfile.ClassId == classId),
                CompletedCount = c.Progress.Count(p => p.Completed && p.StudentProfile.ClassId == classId),
                IsExpired = c.EndDate < DateTime.UtcNow
            })
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);

        return Result<List<ChallengeDto>>.Ok(challenges);
    }

    public async Task<Result<bool>> ExtendChallengeDeadlineAsync(
        Guid classId,
        Guid challengeId,
        ExtendChallengeDeadlineRequest request,
        CancellationToken cancellationToken)
    {
        var challenge = await db.Challenges
            .FirstOrDefaultAsync(c =>
                c.Id == challengeId &&
                c.ClassId == classId &&
                c.Class.TeacherId == currentUser.UserId,
                cancellationToken);

        if (challenge is null)
            return Result<bool>.Fail("Challenge not found.");

        challenge.EndDate = request.EndDate;
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<ChallengeStudentProgressDto>>> GetStudentProgressAsync(
        Guid classId,
        Guid challengeId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<ChallengeStudentProgressDto>>.Fail("Class not found.");

        var progress = await db.ChallengeProgress
            .AsNoTracking()
            .Where(cp => cp.ChallengeId == challengeId && cp.Challenge.ClassId == classId)
            .Select(cp => new ChallengeStudentProgressDto
            {
                StudentId = cp.StudentProfileId,
                StudentName = cp.StudentProfile.FirstName + " " + cp.StudentProfile.LastName,
                Started = cp.StartedAt != null,
                Completed = cp.Completed,
                CurrentValue = cp.CurrentValue
            })
            .OrderBy(p => p.StudentName)
            .ToListAsync(cancellationToken);

        return Result<List<ChallengeStudentProgressDto>>.Ok(progress);
    }
}