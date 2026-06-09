using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Translation;

namespace TeacherDiary.Application.Abstractions.Services;

public interface ITranslationService
{
    Task<Result<TranslateResponseDto>> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken);
}
