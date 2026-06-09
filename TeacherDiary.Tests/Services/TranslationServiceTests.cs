using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TeacherDiary.Infrastructure.Services;
using Xunit;

namespace TeacherDiary.Tests.Services;

public class TranslationServiceTests
{
    private static HttpClient MakeClient(HttpStatusCode status, string body)
    {
        var handler = new FakeHandler(status, body);
        return new HttpClient(handler) { BaseAddress = new Uri("https://api-free.deepl.com/v2/") };
    }

    private static IConfiguration MakeConfig(string? apiKey)
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["DeepL:ApiKey"]).Returns(apiKey);
        return config.Object;
    }

    private static TranslationService CreateService(string? apiKey, HttpStatusCode status, string body)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("DeepL")).Returns(MakeClient(status, body));
        return new TranslationService(factory.Object, MakeConfig(apiKey), NullLogger<TranslationService>.Instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TranslateAsync_WhenTextEmpty_ReturnsFail(string? text)
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{}");

        var result = await service.TranslateAsync(text!, "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Text cannot be empty.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTargetLanguageNull_ReturnsUnsupported()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{}");

        var result = await service.TranslateAsync("Hello", null!, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Unsupported language.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTargetLanguageUnsupported_ReturnsUnsupported()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{}");

        var result = await service.TranslateAsync("Hello", "FR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Unsupported language.", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TranslateAsync_WhenApiKeyMissing_ReturnsNotConfigured(string? apiKey)
    {
        var service = CreateService(apiKey, HttpStatusCode.OK, "{}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation service is not configured.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenDeepLReturnsNonSuccess_ReturnsFail()
    {
        var service = CreateService("test-key", HttpStatusCode.Forbidden, "{}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation failed.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslationsArrayEmpty_ReturnsFail()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"translations\":[]}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation failed.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslationsPropertyMissing_ReturnsFail()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"other\":1}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation failed.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslationsNotArray_ReturnsFail()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"translations\":{}}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation failed.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTextPropertyMissing_ReturnsFail()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"translations\":[{\"detected_source_language\":\"EN\"}]}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation failed.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenTranslatedTextEmpty_ReturnsFail()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"translations\":[{\"text\":\"\"}]}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Translation failed.", result.Error);
    }

    [Fact]
    public async Task TranslateAsync_WhenValidResponse_ReturnsTranslatedText()
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"translations\":[{\"text\":\"Merhaba\"}]}");

        var result = await service.TranslateAsync("Hello", "TR", CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Merhaba", result.Data!.TranslatedText);
        Assert.Equal("TR", result.Data.TargetLanguage);
    }

    [Theory]
    [InlineData("TR")]
    [InlineData("EN")]
    [InlineData("UK")]
    [InlineData("RU")]
    [InlineData("RO")]
    [InlineData("EL")]
    [InlineData("DE")]
    public async Task TranslateAsync_WhenSupportedLanguage_Succeeds(string language)
    {
        var service = CreateService("test-key", HttpStatusCode.OK, "{\"translations\":[{\"text\":\"X\"}]}");

        var result = await service.TranslateAsync("Hello", language, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(language, result.Data!.TargetLanguage);
    }

    private class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
