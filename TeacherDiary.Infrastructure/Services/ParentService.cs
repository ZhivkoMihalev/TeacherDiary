using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
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
                ClassId = s.ClassId.Value
            })
            .ToListAsync(cancellationToken);

        return Result<List<StudentDto>>.Ok(students);
    }

    public async Task<Result<StudentDetailsDto>> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        var student = await db.Students
            .FirstOrDefaultAsync(s =>
                    s.Id == studentId &&
                    s.ParentId == currentUser.UserId,
                cancellationToken);

        if (student is null)
            return Result<StudentDetailsDto>.Fail($"Student with id {studentId} was not found.");

        var details = new StudentDetailsDto
        {
            StudentId = student.Id,
            StudentName = $"{student.FirstName} {student.LastName}",
            IsActive = student.IsActive
        };

        return Result<StudentDetailsDto>.Ok(details);
    }
}

