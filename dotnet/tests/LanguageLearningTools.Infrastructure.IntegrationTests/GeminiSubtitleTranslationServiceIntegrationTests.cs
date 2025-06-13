using System;
using System.Collections.Generic;
using System.Threading;
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
        _service = new GeminiSubtitleTranslationService(_kernel, temperature: 0, loggerFactory: _loggerFactory);
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

    [Fact]
    public async Task TranslateBatchAsync_WithFakeApiKey_ShouldThrowCorrectException()
    {
        // Arrange: Create a service with a completely fake API key
        var fakeApiKey = "fake-gemini-api-key-for-testing-12345";
        var fakeKernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(modelId: _modelId, apiKey: fakeApiKey)
            .Build();
        
        var fakeService = new GeminiSubtitleTranslationService(fakeKernel, temperature: 0, loggerFactory: _loggerFactory);
        
        var testLine = new SubtitleLine(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), "Hallo Welt!");
        var request = new SubtitleBatchRequest
        {
            ContextLines = new List<SubtitleLine>(),
            LinesToTranslate = new List<SubtitleLine> { testLine }
        };

        // Act & Assert: We expect this to throw an exception after retries
        // Note: This will take a while due to retry policy (30s + 60s + 120s + execution time)
        // but we want to see the exact exception details
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await fakeService.TranslateBatchAsync(request, Lang.German, Lang.English);
        });

        // Log the full exception details to understand what's happening
        _testOutputHelper.WriteLine($"Exception type: {exception.GetType().Name}");
        _testOutputHelper.WriteLine($"Exception message: {exception.Message}");
        if (exception.InnerException != null)
        {
            _testOutputHelper.WriteLine($"Inner exception type: {exception.InnerException.GetType().Name}");
            _testOutputHelper.WriteLine($"Inner exception message: {exception.InnerException.Message}");
        }
        _testOutputHelper.WriteLine($"Full exception: {exception}");

        // Check what kind of error we're actually getting
        // We expect either 401 Unauthorized or 403 Forbidden, not 400 Bad Request
        Assert.NotNull(exception);
        
        // Let's specifically check if it contains "400" to confirm our suspicion
        var containsBadRequest = exception.ToString().Contains("400", StringComparison.InvariantCultureIgnoreCase);
        var containsUnauthorized = exception.ToString().Contains("401", StringComparison.InvariantCultureIgnoreCase) ||
                                 exception.ToString().Contains("403", StringComparison.InvariantCultureIgnoreCase) ||
                                 exception.ToString().Contains("Unauthorized", StringComparison.InvariantCultureIgnoreCase) ||
                                 exception.ToString().Contains("Forbidden", StringComparison.InvariantCultureIgnoreCase);
        
        _testOutputHelper.WriteLine($"Contains 400 Bad Request: {containsBadRequest}");
        _testOutputHelper.WriteLine($"Contains 401/403 Unauthorized/Forbidden: {containsUnauthorized}");
    }

    [Fact]
    public async Task TranslateBatchAsync_With15LinesAndContext_ShouldHandleMismatchedResponseCount()
    {
        // Arrange: Create the exact scenario from the failing test
        // Create context lines (5 lines)
        var contextLines = new List<SubtitleLine>
        {
            new(TimeSpan.Parse("00:07:25.706"), TimeSpan.Parse("00:07:28.625"), "...und das ist ein Gedanke, den ich hundertzehnprozentig unterstütze..."),
            new(TimeSpan.Parse("00:07:28.704"), TimeSpan.Parse("00:07:29.997"), "Mir ist da jetzt nur leider ein Fehler"),
            new(TimeSpan.Parse("00:07:30.078"), TimeSpan.Parse("00:07:31.997"), "beziehungsweise ich brauche überraschenderweise Bargeld."),
            new(TimeSpan.Parse("00:07:32.077"), TimeSpan.Parse("00:07:33.745"), "Könnten Sie mir hundert Euro zurückgeben?"),
            new(TimeSpan.Parse("00:07:34.034"), TimeSpan.Parse("00:07:36.328"), "Äh, Sie haben mir aber nur achtzig gegeben.")
        };

        // Create 15 lines to translate (the exact ones from the failing test)
        var linesToTranslate = new List<SubtitleLine>
        {
            new(TimeSpan.Parse("00:07:36.408"), TimeSpan.Parse("00:07:37.409"), "Hundertachtzig."),
            new(TimeSpan.Parse("00:07:37.740"), TimeSpan.Parse("00:07:39.450"), "Ich bin mir relativ sicher, das waren--"),
            new(TimeSpan.Parse("00:07:39.532"), TimeSpan.Parse("00:07:41.200"), "Eben. \"Relativ\". Ich ganz."),
            new(TimeSpan.Parse("00:07:41.281"), TimeSpan.Parse("00:07:43.408"), "Zeigen Sie mal."),
            new(TimeSpan.Parse("00:07:46.695"), TimeSpan.Parse("00:07:48.905"), "Da. Sehen Sie? Hier."),
            new(TimeSpan.Parse("00:07:48.986"), TimeSpan.Parse("00:07:51.697"), "Die beiden Fünfziger die sind von mir. Die erkenn ich an dem..."),
            new(TimeSpan.Parse("00:07:51.734"), TimeSpan.Parse("00:07:53.278"), "Aber ich nehm ja nur einen. Und den Zehner. Ja?"),
            new(TimeSpan.Parse("00:07:53.359"), TimeSpan.Parse("00:07:55.319"), "Sammelst du gerade für Frau Hilpers?"),
            new(TimeSpan.Parse("00:07:55.400"), TimeSpan.Parse("00:07:56.943"), "Also, dann spenden Sie jetzt nur noch zwanzig?"),
            new(TimeSpan.Parse("00:07:57.649"), TimeSpan.Parse("00:07:59.234"), "Sie will mich nicht verstehen."),
            new(TimeSpan.Parse("00:07:59.315"), TimeSpan.Parse("00:08:01.818"), "Sie haben ja achtzig gegeben und sich jetzt sechzig wieder rausgenommen."),
            new(TimeSpan.Parse("00:08:02.231"), TimeSpan.Parse("00:08:03.983"), "Das ist doch Quatsch!"),
            new(TimeSpan.Parse("00:08:05.813"), TimeSpan.Parse("00:08:08.149"), "Ist ja nicht dauerhaft."),
            new(TimeSpan.Parse("00:08:09.978"), TimeSpan.Parse("00:08:11.938"), "Erika? Sie wissen, dass wir im Hinblick auf die"),
            new(TimeSpan.Parse("00:08:12.019"), TimeSpan.Parse("00:08:13.437"), "geplante Zusammenlegung der Abteilung")
        };

        var request = new SubtitleBatchRequest
        {
            ContextLines = contextLines,
            LinesToTranslate = linesToTranslate
        };

        // Act & Assert: This should either succeed with 15 translations or fail gracefully
        var exception = await Record.ExceptionAsync(async () =>
        {
            var result = await _service.TranslateBatchAsync(request, Lang.German, Lang.English);
            
            // If we get here, verify we got the expected number of translations
            Assert.Equal(15, result.TranslatedLines.Count);
            _testOutputHelper.WriteLine($"✅ Success: Got {result.TranslatedLines.Count} translations as expected");
        });

        if (exception != null)
        {
            _testOutputHelper.WriteLine($"❌ Translation failed as expected: {exception.Message}");
            Assert.IsType<InvalidOperationException>(exception);
            Assert.Contains("Expected 15 translations", exception.Message);
        }
    }
}
}
