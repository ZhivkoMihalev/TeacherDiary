using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Book;
using TeacherDiary.Application.DTOs.Reading;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Domain.Enums;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class ReadingService(
    AppDbContext db,
    ICurrentUser currentUser,
    IActivityService activityService,
    ILearningActivityService learningActivityService) : IReadingService
{
    public async Task<Result<Guid>> CreateBookAsync(BookCreateRequest request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId == Guid.Empty) return Result<Guid>.Fail("Unauthorized.");

        var exists = await db.Books.AnyAsync(
            b => b.Title == request.Title && b.Author == request.Author,
            cancellationToken);

        if (exists)
            return Result<Guid>.Fail($"Book {request.Title} already exists.");

        var book = new Book
        {
            Title = request.Title,
            Author = request.Author,
            GradeLevel = request.GradeLevel,
            IsGlobal = request.IsGlobal,
            TotalPages = request.TotalPages,
            CreatedByTeacherId = currentUser.UserId
        };

        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(book.Id);
    }

    public async Task<Result<Guid>> AssignBookToClassAsync(
        Guid classId,
        AssignBookRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
            return Result<Guid>.Fail("Invalid date range.");

        var currentClass = await db.Classes.FirstOrDefaultAsync(
            c => c.Id == classId &&
                 c.OrganizationId == currentUser.OrganizationId &&
                 c.TeacherId == currentUser.UserId,
            cancellationToken);

        if (currentClass is null)
            return Result<Guid>.Fail($"Class with id: {classId} was not found.");

        var book = await db.Books
            .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);

        if (book is null)
            return Result<Guid>.Fail("Book not found.");

        var totalPages = book.TotalPages;

        var assigned = new AssignedBook
        {
            ClassId = currentClass.Id,
            BookId = book.Id,
            StartDateUtc = request.StartDate,
            EndDateUtc = request.EndDate
        };

        db.AssignedBooks.Add(assigned);
        await db.SaveChangesAsync(cancellationToken);

        var studentIds = await db.Students
            .Where(s => s.ClassId == currentClass.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var progressRows = studentIds.Select(sid => new ReadingProgress
        {
            StudentProfileId = sid,
            AssignedBookId = assigned.Id,
            Status = ProgressStatus.NotStarted,
            CurrentPage = 0,
            TotalPages = totalPages
        });

        db.ReadingProgress.AddRange(progressRows);
        await db.SaveChangesAsync(cancellationToken);

        await learningActivityService.CreateForAssignedBookAsync(
            assigned,
            cancellationToken);

        return Result<Guid>.Ok(assigned.Id);
    }

    public async Task<Result<bool>> UpdateProgressAsync(
        Guid studentId,
        Guid assignedBookId,
        int currentPage,
        CancellationToken cancellationToken)
    {
        if (currentPage < 0)
            return Result<bool>.Fail("Current page cannot be negative.");

        var student = await db.Students
            .FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

        if (student is null)
            return Result<bool>.Fail($"Student with id {studentId} was not found.");

        if (student.ParentId != currentUser.UserId)
            return Result<bool>.Fail("Forbidden.");

        var progress = await db.ReadingProgress
            .FirstOrDefaultAsync(p =>
                    p.StudentProfileId == studentId &&
                    p.AssignedBookId == assignedBookId,
                cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Progress not found.");

        var previousPage = progress.CurrentPage;

        if (progress.StartedAt == null)
            progress.StartedAt = DateTime.UtcNow;

        if (currentPage < progress.CurrentPage)
            return Result<bool>.Fail("Current page cannot be less than previous progress.");

        progress.CurrentPage = currentPage;
        progress.LastUpdatedAt = DateTime.UtcNow;

        if (progress.TotalPages.HasValue &&
            currentPage >= progress.TotalPages.Value)
        {
            progress.Status = ProgressStatus.Completed;
            progress.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            progress.Status = ProgressStatus.InProgress;
        }

        var pagesDelta = currentPage - previousPage;
        var bookCompleted = progress.Status == ProgressStatus.Completed;

        await activityService.LogReadingAsync(
            studentId,
            assignedBookId,
            pagesDelta,
            bookCompleted,
            cancellationToken);

        await learningActivityService.UpdateReadingProgressAsync(
            studentId,
            assignedBookId,
            currentPage,
            cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Result<bool>.Ok(true);
    }

    public async Task<Result<List<BookDto>>> GetBooksAsync(
        int? gradeLevel,
        CancellationToken cancellationToken)
    {
        var query = db.Books.AsNoTracking();

        if (gradeLevel.HasValue)
            query = query.Where(b => b.GradeLevel == gradeLevel);

        var books = await query
            .OrderBy(b => b.Title)
            .Select(b => new BookDto
            {
                Id = b.Id,
                Title = b.Title,
                Author = b.Author,
                GradeLevel = b.GradeLevel
            })
            .ToListAsync(cancellationToken);

        return Result<List<BookDto>>.Ok(books);
    }

    public async Task<Result<List<AssignedBookDto>>> GetAssignedBooksAsync(
        Guid classId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<AssignedBookDto>>.Fail($"Class with id {classId} was not found.");

        var books = await db.AssignedBooks
            .Where(b => b.ClassId == classId)
            .Select(b => new AssignedBookDto
            {
                AssignedBookId = b.Id,
                BookId = b.BookId,
                Title = b.Book.Title,
                Author = b.Book.Author,
                StartDate = b.StartDateUtc,
                EndDate = b.EndDateUtc,
                StudentsReading = b.ReadingProgress.Count(p => p.Status == ProgressStatus.InProgress),
                StudentsCompleted = b.ReadingProgress.Count(p => p.Status == ProgressStatus.Completed)
            })
            .OrderByDescending(b => b.StartDate)
            .ToListAsync(cancellationToken);

        return Result<List<AssignedBookDto>>.Ok(books);
    }
}
