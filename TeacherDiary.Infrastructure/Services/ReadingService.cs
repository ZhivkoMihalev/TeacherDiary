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
    ILearningActivityService learningActivityService,
    IBadgeService badgeService) : IReadingService
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
        if (request.StartDateUtc > request.EndDateUtc)
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
            StartDateUtc = request.StartDateUtc,
            EndDateUtc = request.EndDateUtc,
            Points = request.Points
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
            .Include(p => p.AssignedBook)
            .FirstOrDefaultAsync(p =>
                    p.StudentProfileId == studentId &&
                    p.AssignedBookId == assignedBookId,
                cancellationToken);

        if (progress is null)
            return Result<bool>.Fail("Progress not found.");

        if (progress.AssignedBook.EndDateUtc.HasValue &&
            progress.AssignedBook.EndDateUtc.Value < DateTime.UtcNow)
            return Result<bool>.Fail("Deadline has passed. Reading progress is locked.");

        var previousPage = progress.CurrentPage;
        var wasAlreadyCompleted = progress.Status == ProgressStatus.Completed;

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
            if (!wasAlreadyCompleted)
                progress.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            progress.Status = ProgressStatus.InProgress;
        }

        var pagesDelta = currentPage - previousPage;
        var bookCompleted = !wasAlreadyCompleted && progress.Status == ProgressStatus.Completed;

        await activityService.LogReadingAsync(
            studentId,
            assignedBookId,
            pagesDelta,
            bookCompleted,
            progress.AssignedBook.Points,
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
                GradeLevel = b.GradeLevel,
                TotalPages = b.TotalPages ?? 0
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
                Id = b.Id,
                BookId = b.BookId,
                Title = b.Book.Title,
                Author = b.Book.Author,
                TotalPages = b.Book.TotalPages ?? 0,
                StartDateUtc = b.StartDateUtc,
                EndDateUtc = b.EndDateUtc,
                Points = b.Points,
                NotStartedCount = b.ReadingProgress.Count(p => p.Status == ProgressStatus.NotStarted && p.StudentProfile.ClassId == classId),
                InProgressCount = b.ReadingProgress.Count(p => p.Status == ProgressStatus.InProgress && p.StudentProfile.ClassId == classId),
                CompletedCount = b.ReadingProgress.Count(p => p.Status == ProgressStatus.Completed && p.StudentProfile.ClassId == classId),
                IsExpired = b.EndDateUtc.HasValue && b.EndDateUtc.Value < DateTime.UtcNow
            })
            .OrderByDescending(b => b.StartDateUtc)
            .ToListAsync(cancellationToken);

        return Result<List<AssignedBookDto>>.Ok(books);
    }

    public async Task<Result<List<AssignedBookStudentProgressDto>>> GetStudentProgressForBookAsync(
        Guid classId,
        Guid assignedBookId,
        CancellationToken cancellationToken)
    {
        var classExists = await db.Classes.AnyAsync(c =>
                c.Id == classId &&
                c.TeacherId == currentUser.UserId &&
                c.OrganizationId == currentUser.OrganizationId,
            cancellationToken);

        if (!classExists)
            return Result<List<AssignedBookStudentProgressDto>>.Fail("Class not found.");

        var progress = await db.ReadingProgress
            .Where(p => p.AssignedBookId == assignedBookId &&
                        p.StudentProfile.ClassId == classId)
            .Select(p => new AssignedBookStudentProgressDto
            {
                StudentId = p.StudentProfileId,
                StudentName = p.StudentProfile.FirstName + " " + p.StudentProfile.LastName,
                CurrentPage = p.CurrentPage,
                TotalPages = p.TotalPages ?? 0,
                Status = p.Status
            })
            .OrderBy(p => p.StudentName)
            .ToListAsync(cancellationToken);

        return Result<List<AssignedBookStudentProgressDto>>.Ok(progress);
    }

    public async Task<Result<bool>> UpdateAssignedBookAsync(
        Guid classId,
        Guid assignedBookId,
        UpdateAssignedBookRequest request,
        CancellationToken cancellationToken)
    {
        if (request.StartDateUtc > request.EndDateUtc)
            return Result<bool>.Fail("Start date cannot be after end date.");

        var assigned = await db.AssignedBooks
            .FirstOrDefaultAsync(ab =>
                ab.Id == assignedBookId &&
                ab.ClassId == classId &&
                ab.Class.TeacherId == currentUser.UserId,
                cancellationToken);

        if (assigned is null)
            return Result<bool>.Fail("Assigned book not found.");

        int pointsDelta = request.Points - assigned.Points;

        assigned.StartDateUtc = request.StartDateUtc;
        assigned.EndDateUtc = request.EndDateUtc;
        assigned.Points = request.Points;

        if (pointsDelta != 0)
        {
            var completedStudentIds = await db.ReadingProgress
                .Where(p => p.AssignedBookId == assignedBookId && p.Status == ProgressStatus.Completed)
                .Select(p => p.StudentProfileId)
                .ToListAsync(cancellationToken);

            foreach (var studentId in completedStudentIds)
            {
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

                // Update the completion log entry (highest PointsEarned, then most recent)
                var log = await db.ActivityLogs
                    .Where(a =>
                        a.StudentProfileId == studentId &&
                        a.ActivityType == ActivityType.ReadingProgress &&
                        a.ReferenceId == assignedBookId)
                    .OrderByDescending(a => a.PointsEarned)
                    .ThenByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (log is not null)
                    log.PointsEarned = Math.Max(0, (log.PointsEarned ?? 0) + pointsDelta);

                await badgeService.EvaluateAsync(studentId, cancellationToken);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> RemoveAssignedBookAsync(
        Guid classId,
        Guid assignedBookId,
        CancellationToken cancellationToken)
    {
        var assigned = await db.AssignedBooks
            .FirstOrDefaultAsync(ab =>
                ab.Id == assignedBookId &&
                ab.ClassId == classId &&
                ab.Class.TeacherId == currentUser.UserId,
                cancellationToken);

        if (assigned is null)
            return Result<bool>.Fail("Assigned book not found.");

        await db.ReadingProgress
            .Where(p => p.AssignedBookId == assignedBookId)
            .ExecuteDeleteAsync(cancellationToken);

        var activityIds = await db.LearningActivities
            .Where(la => la.AssignedBookId == assignedBookId)
            .Select(la => la.Id)
            .ToListAsync(cancellationToken);

        if (activityIds.Count > 0)
        {
            await db.StudentLearningActivityProgress
                .Where(p => activityIds.Contains(p.LearningActivityId))
                .ExecuteDeleteAsync(cancellationToken);

            await db.LearningActivities
                .Where(la => activityIds.Contains(la.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        db.AssignedBooks.Remove(assigned);
        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }

    public async Task<Result<bool>> UpdateBookAsync(Guid bookId, BookUpdateRequest request, CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == bookId, cancellationToken);

        if (book is null)
            return Result<bool>.Fail($"Book with id {bookId} was not found.");

        book.Title = request.Title;
        book.Author = request.Author;
        book.GradeLevel = request.GradeLevel;
        book.TotalPages = request.TotalPages;

        await db.SaveChangesAsync(cancellationToken);
        return Result<bool>.Ok(true);
    }
}
