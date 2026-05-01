using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Messages;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize]
public class MessagesController(IMessageService messages, IWebHostEnvironment env) : ControllerBase
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

    /// <summary>
    /// Returns the list of users the current user can message.
    /// </summary>
    /// <remarks>
    /// For teachers: returns all parents of students in their classes.
    /// For parents: returns the teachers of their students' classes.
    /// For students: returns their class teacher.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of contacts with user ID and display name.</returns>
    /// <response code="200">Returns the contact list.</response>
    /// <response code="400">Unexpected service error.</response>
    [HttpGet("api/messages/contacts")]
    public async Task<IActionResult> GetContacts(CancellationToken cancellationToken)
    {
        var result = await messages.GetContactsAsync(cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns the number of unread messages for the current user.
    /// </summary>
    /// <remarks>
    /// Used by the sidebar to display the unread message badge.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Count of unread messages.</returns>
    /// <response code="200">Returns the unread count.</response>
    /// <response code="400">Unexpected service error.</response>
    [HttpGet("api/messages/unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var result = await messages.GetUnreadCountAsync(cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns all conversations for the current user.
    /// </summary>
    /// <remarks>
    /// Returns a summary of each conversation thread, including:
    /// - the other participant's name and ID
    /// - the most recent message text and timestamp
    /// - count of unread messages in the thread
    /// Results are sorted by most recent message descending.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of conversation summaries.</returns>
    /// <response code="200">Returns the conversation list.</response>
    /// <response code="400">Unexpected service error.</response>
    [HttpGet("api/messages/conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var result = await messages.GetConversationsAsync(cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Returns the message history with a specific user.
    /// </summary>
    /// <remarks>
    /// Returns all messages exchanged between the current user and <paramref name="otherUserId"/>,
    /// sorted chronologically (oldest first). Reading this endpoint marks all messages from the
    /// other user as read.
    /// </remarks>
    /// <param name="otherUserId">ID of the other participant in the conversation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered list of messages in the conversation.</returns>
    /// <response code="200">Returns the message list.</response>
    /// <response code="400">Other user not found or not a valid contact.</response>
    [HttpGet("api/messages/conversations/{otherUserId:guid}")]
    public async Task<IActionResult> GetConversation(Guid otherUserId, CancellationToken cancellationToken)
    {
        var result = await messages.GetConversationAsync(otherUserId, cancellationToken);
        return result.Success 
            ? Ok(result.Data) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Sends a message to another user.
    /// </summary>
    /// <remarks>
    /// Creates a message in the conversation thread between the current user and the recipient.
    /// The message is delivered in real-time via SignalR if the recipient is connected.
    /// Supports plain text and image URLs (use <c>POST /api/messages/upload-image</c> first to get an image URL).
    /// </remarks>
    /// <param name="request">Message data (recipient user ID, text content or image URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the newly created message.</returns>
    /// <response code="200">Message sent — returns <c>{ messageId }</c>.</response>
    /// <response code="400">Recipient not found or validation error.</response>
    [HttpPost("api/messages")]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await messages.SendMessageAsync(request, cancellationToken);
        return result.Success 
            ? Ok(new { messageId = result.Data }) 
            : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Uploads an image to be attached to a message.
    /// </summary>
    /// <remarks>
    /// Accepts JPEG, PNG, GIF, or WEBP files up to 5 MB.
    /// Returns a URL that can be passed as the <c>imageUrl</c> field when sending a message via
    /// <c>POST /api/messages</c>. The file is saved to <c>wwwroot/uploads/messages/</c>.
    /// </remarks>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The relative URL of the uploaded image.</returns>
    /// <response code="200">Upload successful — returns <c>{ imageUrl }</c>.</response>
    /// <response code="400">No file provided, file too large, or unsupported content type.</response>
    [HttpPost("api/messages/upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Не е избрано изображение." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { error = "Изображението е прекалено голямо (макс. 5MB)." });

        if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { error = "Разрешени са само изображения (JPEG, PNG, GIF, WEBP)." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext))
            ext = file.ContentType.ToLower() switch
            {
                "image/jpeg" => ".jpg",
                "image/png"  => ".png",
                "image/gif"  => ".gif",
                "image/webp" => ".webp",
                _            => ".bin"
            };

        var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads", "messages");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return Ok(new { imageUrl = $"/uploads/messages/{fileName}" });
    }
}
