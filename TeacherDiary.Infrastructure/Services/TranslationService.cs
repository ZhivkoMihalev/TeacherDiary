using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeacherDiary.Application.Abstractions.Services;
using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Translation;

namespace TeacherDiary.Infrastructure.Services;

public sealed class TranslationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<TranslationService> logger) : ITranslationService
{
    private static readonly HashSet<string> SupportedLanguages =
        new(StringComparer.OrdinalIgnoreCase) { "TR", "EN", "UK", "RU", "RO", "EL", "DE" };

    public async Task<Result<TranslateResponseDto>> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result<TranslateResponseDto>.Fail("Text cannot be empty.");

        if (string.IsNullOrWhiteSpace(targetLanguage) || !SupportedLanguages.Contains(targetLanguage))
            return Result<TranslateResponseDto>.Fail("Unsupported language.");

        var apiKey = configuration["DeepL:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Result<TranslateResponseDto>.Fail("Translation service is not configured.");

        var client = httpClientFactory.CreateClient("DeepL");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["text"] = text,
            ["target_lang"] = targetLanguage
        });

        var response = await client.PostAsync("translate", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("DeepL translation request failed with status {StatusCode}.", response.StatusCode);
            return Result<TranslateResponseDto>.Fail("Translation failed.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("translations", out var translations)
            || translations.ValueKind != JsonValueKind.Array
            || translations.GetArrayLength() == 0
            || !translations[0].TryGetProperty("text", out var translatedTextElement))
        {
            return Result<TranslateResponseDto>.Fail("Translation failed.");
        }

        var translatedText = translatedTextElement.GetString();
        if (string.IsNullOrEmpty(translatedText))
            return Result<TranslateResponseDto>.Fail("Translation failed.");

        return Result<TranslateResponseDto>.Ok(new TranslateResponseDto(translatedText, targetLanguage));
    }
}
