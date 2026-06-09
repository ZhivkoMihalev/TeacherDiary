using System.ComponentModel.DataAnnotations;

namespace TeacherDiary.Application.DTOs.Translation;

public record TranslateRequestDto(
    [MaxLength(5000)] string Text,
    string TargetLanguage);
