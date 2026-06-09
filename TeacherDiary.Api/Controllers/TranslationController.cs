using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.DTOs.Translation;

namespace TeacherDiary.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/translation")]
public class TranslationController(ITranslationService translationService) : ControllerBase
{
    /// <summary>Translates the given text into one of the supported languages via DeepL.</summary>
    /// <param name="request">The text and target language code (TR, EN, UK, RU, RO, EL, DE).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The translated text and the target language code.</returns>
    /// <response code="200">Returns the translated text.</response>
    /// <response code="400">Invalid request, unsupported language, or translation failed.</response>
    [HttpPost("translate")]
    public async Task<IActionResult> Translate(
        [FromBody] TranslateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await translationService.TranslateAsync(request.Text, request.TargetLanguage, cancellationToken);
        return result.Success
            ? Ok(result.Data)
            : BadRequest(new { error = result.Error });
    }
}
