using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace LanguageLearningTools.Infrastructure.IntegrationTests
{
    /// <summary>
    /// Simple logger factory for xUnit tests
    /// </summary>
    public class XUnitLoggerFactory : ILoggerFactory
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LogLevel _minLogLevel;

        public XUnitLoggerFactory(ITestOutputHelper testOutputHelper, LogLevel minLogLevel = LogLevel.Debug)
        {
            _testOutputHelper = testOutputHelper;
            _minLogLevel = minLogLevel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, categoryName, _minLogLevel);
        }

        public void AddProvider(ILoggerProvider provider) { }
        public void Dispose() { }
    }

    /// <summary>
    /// Simple logger provider that outputs to xUnit test output
    /// </summary>
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_testOutputHelper, categoryName);
        }

        public void Dispose() { }
    }

    /// <summary>
    /// Simple logger that outputs to xUnit test output
    /// </summary>
    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _categoryName;
        private readonly LogLevel _minLogLevel;

        public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName, LogLevel minLogLevel = LogLevel.Debug)
        {
            _testOutputHelper = testOutputHelper;
            _categoryName = categoryName;
            _minLogLevel = minLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                var message = formatter(state, exception);
                _testOutputHelper.WriteLine($"[{logLevel}] {_categoryName}: {message}");
                
                if (exception != null)
                {
                    _testOutputHelper.WriteLine($"Exception: {exception}");
                }
            }
            catch
            {
                // Swallow exceptions to prevent test failures due to logging issues
            }
        }
    }

public class GeminiSubtitleTranslationServiceIntegrationTests
{
    private readonly string _apiKey;
    private readonly string _modelId = "gemini-2.5-flash-preview-05-20";
    private readonly Kernel _kernel;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GeminiSubtitleTranslationService _service;
    private readonly ITestOutputHelper _testOutputHelper;

    public GeminiSubtitleTranslationServiceIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        
        var config = new ConfigurationBuilder()
            .AddUserSecrets<GeminiSubtitleTranslationServiceIntegrationTests>()
            .Build();
        _apiKey = config["GEMINI_API_KEY"];
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("GEMINI_API_KEY must be set in user secrets.");
        
        // Create logger factory that outputs to xUnit test output
        // Change this to see debug logs during test execution
        _loggerFactory = CreateLoggerFactory(enableDebugLogging: true);
        
        _kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(modelId: _modelId, apiKey: _apiKey)
            .Build();
        
        // For test determinism, set temperature to 0
        _service = new GeminiSubtitleTranslationService(_kernel, temperature: 0, _loggerFactory);
    }

    /// <summary>
    /// Creates a logger factory for tests. Set enableDebugLogging to true to see detailed logs during test execution.
    /// </summary>
    /// <param name="enableDebugLogging">If true, enables debug and information level logging to test output</param>
    /// <returns>A configured ILoggerFactory</returns>
    private ILoggerFactory CreateLoggerFactory(bool enableDebugLogging)
    {
        if (!enableDebugLogging)
        {
            // Use NullLoggerFactory for tests (no console output during tests)
            return Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        }

        // Create a simple logger factory that outputs to xUnit test output
        return new XUnitLoggerFactory(_testOutputHelper, LogLevel.Debug);
    }

    /// <summary>
    /// Directly tests the Gemini kernel by sending a simple translation prompt and checking the response.
    /// This test bypasses the translation service and verifies that the Gemini kernel is operational.
    /// </summary>
    [Fact(DisplayName = "Gemini Kernel Direct Test: Translate 'Hello, world!' to German (raw kernel)")]
    public async Task GeminiKernel_DirectPrompt_Works()
    {
        // Arrange
        var prompt = @"Translate the following English text into German, ensuring accuracy and natural phrasing.
        Maintain proper grammar and adapt idioms or expressions where necessary for clarity.
        Provide the output in JSON format:
        { 'original': 'Hello, world!', 'translated': '[translated German text]' }";

        // Act
        var result = await _kernel.InvokePromptAsync(prompt);
        var output = result.ToString();
        Assert.False(string.IsNullOrWhiteSpace(output));

        // Remove Markdown code block using regex if present
        string json = output.Trim();
        var match = System.Text.RegularExpressions.Regex.Match(json, @"```(?:json)?\s*([\s\S]*?)\s*```", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            json = match.Groups[1].Value.Trim();
        }
        // Replace single quotes with double quotes for valid JSON
        json = json.Replace("'", "\"");

        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        Assert.NotNull(dict);
        Assert.True(dict.ContainsKey("original"));
        Assert.True(dict.ContainsKey("translated"));
        Assert.Equal("Hello, world!", dict["original"]);
        Assert.Equal("Hallo, Welt!", dict["translated"]);
    }


    [Fact]
    public async Task TranslateBatchAsync_Should_Translate_English_To_German_WithStructuredOutput()
    {
        // Parse the TTML file from output directory (fixture)
        var parser = new TtmlSubtitleParser();
        IReadOnlyList<SubtitleLine> allLines;
        var ttmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, "SampleThreeLine.ttml2");
        using (var stream = System.IO.File.OpenRead(ttmlPath))
        {
            allLines = await parser.ParseAsync(stream);
        }
        Assert.True(allLines.Count == 3, "SampleThreeLine.ttml2 should have 3 subtitle lines.");

        // Use line 1 as context, lines 2 and 3 as lines to translate (English input)
        var request = new SubtitleBatchRequest
        {
            ContextLines = new List<SubtitleLine> { allLines[0] },
            LinesToTranslate = new List<SubtitleLine> { allLines[1], allLines[2] }
        };

        // Act
        var response = await _service.TranslateBatchAsync(request, Lang.English, Lang.German);

        // Assert: Check that translations are non-empty, different from input, and in the expected order
        Assert.Equal(request.LinesToTranslate.Count, response.TranslatedLines.Count);
        Assert.NotEqual(request.LinesToTranslate[0].Text.Trim(), response.TranslatedLines[0].TranslatedText.Trim());
        Assert.NotEqual(request.LinesToTranslate[1].Text.Trim(), response.TranslatedLines[1].TranslatedText.Trim());
        // Accept both singular and plural, with or without umlaut, for robustness
        var rainbowVariants = new[] { "Regenbogen", "Regenbögen" };
        Assert.Contains(rainbowVariants,
            variant => response.TranslatedLines[0].TranslatedText.Contains(variant, StringComparison.InvariantCultureIgnoreCase));

        // Pizza is the same in German, but check for presence
        Assert.Contains("Pizza", response.TranslatedLines[1].TranslatedText, System.StringComparison.InvariantCultureIgnoreCase);

        // Additional: Ensure output is not English (basic check)
        Assert.DoesNotContain(request.LinesToTranslate[0].Text, response.TranslatedLines[0].TranslatedText, StringComparison.InvariantCultureIgnoreCase);
        Assert.DoesNotContain(request.LinesToTranslate[1].Text, response.TranslatedLines[1].TranslatedText, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task TranslateBatchAsync_Should_Preserve_Emojis_And_SpecialChars()
    {
        // Test lines with emojis and special characters
        var emojiLine = new SubtitleLine(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), "🦄 Rainbows are just unicorn sneezes! 🌈");
        var specialCharLine = new SubtitleLine(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), "Café déjà vu – naïve façade! ¿Cómo estás? 你好！");
        var request = new SubtitleBatchRequest
        {
            ContextLines = new List<SubtitleLine>(),
            LinesToTranslate = new List<SubtitleLine> { emojiLine, specialCharLine }
        };

        // Act
        var response = await _service.TranslateBatchAsync(request, Lang.English, Lang.German);

        // Assert: Ensure emojis and special characters are preserved in the translation
        Assert.Equal(request.LinesToTranslate.Count, response.TranslatedLines.Count);
        Assert.Contains("🦄", response.TranslatedLines[0].TranslatedText);
        Assert.Contains("🌈", response.TranslatedLines[0].TranslatedText);
        Assert.Contains("Café", response.TranslatedLines[1].TranslatedText, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("¿", response.TranslatedLines[1].TranslatedText);
        Assert.Contains("！", response.TranslatedLines[1].TranslatedText);
    }
}
}
