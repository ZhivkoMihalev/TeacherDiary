using Microsoft.EntityFrameworkCore;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Messages;
using TeacherDiary.Domain.Entities;
using TeacherDiary.Infrastructure.Persistence;

namespace TeacherDiary.Infrastructure.Services;

public sealed class MessageService(AppDbContext db, ICurrentUser currentUser) : IMessageService
{
    public async Task<Result<List<ConversationDto>>> GetConversationsAsync(CancellationToken cancellationToken)
    {
        var myId = currentUser.UserId;

        var messages = await db.Messages
            .Where(m => m.SenderId == myId || m.ReceiverId == myId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var otherUserIds = messages
            .Select(m => m.SenderId == myId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToList();

        var users = await db.Users
            .Where(u => otherUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);

        var userMap = users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        // For teacher: map parent → student name
        var studentNameMap = new Dictionary<Guid, string>();
        var teacherClassIds = await db.Classes
            .Where(c => c.TeacherId == myId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (teacherClassIds.Count > 0)
        {
            var parentStudents = await db.Students
                .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                .Select(s => new { s.ParentId, StudentName = s.FirstName + " " + s.LastName })
                .ToListAsync(cancellationToken);

            foreach (var ps in parentStudents.Where(ps => ps.ParentId.HasValue))
                studentNameMap.TryAdd(ps.ParentId!.Value, ps.StudentName);
        }

        var conversations = messages
            .GroupBy(m => m.SenderId == myId ? m.ReceiverId : m.SenderId)
            .Select(g =>
            {
                var last = g.First();
                return new ConversationDto
                {
                    OtherUserId = g.Key,
                    OtherUserName = userMap.GetValueOrDefault(g.Key, "Непознат"),
                    StudentName = studentNameMap.GetValueOrDefault(g.Key),
                    LastMessage = last.ImageUrl != null && last.Content == null ? "[Снимка]" : last.Content ?? "",
                    LastMessageIsImage = last.ImageUrl != null && last.Content == null,
                    LastMessageAt = last.CreatedAt,
                    UnreadCount = g.Count(m => m.ReceiverId == myId && !m.IsRead),
                    LastMessageIsFromMe = last.SenderId == myId
                };
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();

        return Result<List<ConversationDto>>.Ok(conversations);
    }

    public async Task<Result<List<MessageDto>>> GetConversationAsync(Guid otherUserId, CancellationToken cancellationToken)
    {
        var myId = currentUser.UserId;

        // Mark incoming messages as read
        await db.Messages
            .Where(m => m.SenderId == otherUserId && m.ReceiverId == myId && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true), cancellationToken);

        var messages = await db.Messages
            .Where(m =>
                (m.SenderId == myId && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == myId))
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                ImageUrl = m.ImageUrl,
                IsFromMe = m.SenderId == myId,
                IsRead = m.IsRead,
                SentAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result<List<MessageDto>>.Ok(messages);
    }

    public async Task<Result<Guid>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.ImageUrl))
            return Result<Guid>.Fail("Съобщението не може да е празно.");

        var message = new Message
        {
            SenderId = currentUser.UserId,
            ReceiverId = request.ReceiverId,
            Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim(),
            ImageUrl = request.ImageUrl,
            IsRead = false
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(message.Id);
    }

    public async Task<Result<int>> GetUnreadCountAsync(CancellationToken cancellationToken)
    {
        var count = await db.Messages
            .CountAsync(m => m.ReceiverId == currentUser.UserId && !m.IsRead, cancellationToken);

        return Result<int>.Ok(count);
    }

    public async Task<Result<List<MessageContactDto>>> GetContactsAsync(CancellationToken cancellationToken)
    {
        var myId = currentUser.UserId;

        // Teacher mode: return parents of students in teacher's classes
        var teacherClassIds = await db.Classes
            .Where(c => c.TeacherId == myId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (teacherClassIds.Count > 0)
        {
            var studentData = await db.Students
                .Where(s => s.ClassId.HasValue && teacherClassIds.Contains(s.ClassId.Value))
                .Select(s => new { s.ParentId, s.UserId, StudentName = s.FirstName + " " + s.LastName })
                .ToListAsync(cancellationToken);

            // Parents of students with a parent account
            var parentMap = studentData
                .Where(p => p.ParentId.HasValue)
                .GroupBy(p => p.ParentId!.Value)
                .ToDictionary(g => g.Key, g => g.First().StudentName);

            var parentUsers = await db.Users
                .Where(u => parentMap.Keys.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync(cancellationToken);

            var parentContacts = parentUsers.Select(u => new MessageContactDto
            {
                UserId = u.Id,
                FullName = $"{u.FirstName} {u.LastName}",
                StudentName = parentMap.GetValueOrDefault(u.Id)
            });

            // Self-registered students (UserId set, no parent)
            var selfStudentUserIds = studentData
                .Where(s => s.UserId.HasValue && !s.ParentId.HasValue)
                .Select(s => s.UserId!.Value)
                .ToList();

            var selfStudentUsers = await db.Users
                .Where(u => selfStudentUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync(cancellationToken);

            var selfStudentData = studentData
                .Where(s => s.UserId.HasValue && !s.ParentId.HasValue)
                .ToDictionary(s => s.UserId!.Value, s => s.StudentName);

            var selfStudentContacts = selfStudentUsers.Select(u => new MessageContactDto
            {
                UserId = u.Id,
                FullName = $"{u.FirstName} {u.LastName}",
                StudentName = selfStudentData.GetValueOrDefault(u.Id)
            });

            var contacts = parentContacts
                .Concat(selfStudentContacts)
                .OrderBy(c => c.FullName)
                .ToList();

            return Result<List<MessageContactDto>>.Ok(contacts);
        }

        // Student mode: return teacher of the class the student is in
        var studentProfile = await db.Students
            .FirstOrDefaultAsync(s => s.UserId == myId && s.ClassId.HasValue, cancellationToken);

        if (studentProfile != null)
        {
            var studentClass = await db.Classes
                .Where(c => c.Id == studentProfile.ClassId!.Value)
                .Select(c => new { c.TeacherId })
                .FirstOrDefaultAsync(cancellationToken);

            if (studentClass != null)
            {
                var teacher = await db.Users
                    .Where(u => u.Id == studentClass.TeacherId)
                    .Select(u => new { u.Id, u.FirstName, u.LastName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (teacher != null)
                {
                    return Result<List<MessageContactDto>>.Ok(new List<MessageContactDto>
                    {
                        new MessageContactDto
                        {
                            UserId = teacher.Id,
                            FullName = $"{teacher.FirstName} {teacher.LastName}"
                        }
                    });
                }
            }

            return Result<List<MessageContactDto>>.Ok(new List<MessageContactDto>());
        }

        // Parent mode: return teachers of classes the parent's children are in
        var classIds = await db.Students
            .Where(s => s.ParentId == myId && s.ClassId.HasValue)
            .Select(s => s.ClassId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        var teacherIds = await db.Classes
            .Where(c => classIds.Contains(c.Id))
            .Select(c => c.TeacherId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var teacherUsers = await db.Users
            .Where(u => teacherIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken);

        var teacherContacts = teacherUsers
            .Select(u => new MessageContactDto
            {
                UserId = u.Id,
                FullName = $"{u.FirstName} {u.LastName}"
            })
            .OrderBy(c => c.FullName)
            .ToList();

        return Result<List<MessageContactDto>>.Ok(teacherContacts);
    }
}
