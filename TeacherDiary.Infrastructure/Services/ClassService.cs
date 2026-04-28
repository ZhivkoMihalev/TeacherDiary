using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Classes;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ClassService(AppDbContext db, ICurrentUser currentUser) : IClassService
{
    public async Task<Result<ClassDto>> CreateAsync(ClassCreateRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == Guid.Empty) return Result<ClassDto>.Fail("Unauthorized.");

        var newClass = new Class
        {
            OrganizationId = currentUser.OrganizationId,
            TeacherId = currentUser.UserId,
            Name = request.Name,
            Grade = request.Grade,
            SchoolYear = request.SchoolYear
        };

        db.Classes.Add(newClass);
        await db.SaveChangesAsync(cancellationToken);

        return Result<ClassDto>.Ok(new ClassDto
        {
            Id = newClass.Id,
            Name = newClass.Name,
            Grade = newClass.Grade,
            SchoolYear = newClass.SchoolYear,
            StudentsCount = 0
        });
    }

    public async Task<Result<List<ClassDto>>> GetMyClassesAsync(CancellationToken cancellationToken)
    {
        var list = await db.Classes
            .AsNoTracking()
            .Where(c => c.OrganizationId == currentUser.OrganizationId 
                        && c.TeacherId == currentUser.UserId)
            .Select(c => new ClassDto
            {
                Id = c.Id,
                Name = c.Name,
                Grade = c.Grade,
                SchoolYear = c.SchoolYear,
                StudentsCount = c.Students.Count
            })
            .ToListAsync(cancellationToken);

        return Result<List<ClassDto>>.Ok(list);
    }

    public async Task<Result<bool>> UpdateAsync(Guid classId, ClassUpdateRequest request, CancellationToken cancellationToken)
    {
        var currentClass = await db.Classes.FirstOrDefaultAsync(
            c => c.Id == classId &&
                 c.OrganizationId == currentUser.OrganizationId &&
                 c.TeacherId == currentUser.UserId,
            cancellationToken);

        if (currentClass is null)
            return Result<bool>.Fail($"Class with id: {classId} was not found.");

        currentClass.Name = request.Name;
        currentClass.Grade = request.Grade;
        currentClass.SchoolYear = request.SchoolYear;

        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> DeleteAsync(Guid classId, CancellationToken cancellationToken)
    {
        var currentClass = await db.Classes.FirstOrDefaultAsync(
            currentClass => currentClass.Id == classId 
                            && currentClass.OrganizationId == currentUser.OrganizationId 
                            && currentClass.TeacherId == currentUser.UserId, cancellationToken);

        if (currentClass is null) return Result<bool>.Fail($"Class with id: {classId} was not found.");

        db.Classes.Remove(currentClass);
        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }
}
