using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Retry;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Translation service using Google Gemini via Semantic Kernel.
    /// </summary>
    public class GeminiSubtitleTranslationService : ISubtitleTranslationService
    {
        private readonly Kernel _kernel;
        private readonly double _temperature;
        private readonly ILogger _logger;
        private readonly TimeSpan _requestDelay;
        private static readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleTranslationService"/> class.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance configured for Gemini.</param>
        /// <param name="temperature">The temperature for Gemini completions (0 = deterministic, 1 = most random). Default is 0.2.</param>
        /// <param name="requestDelay">The minimum delay between API requests to avoid rate limiting. Default is 7.5 seconds for 8 RPM.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
        public GeminiSubtitleTranslationService(Kernel kernel, double temperature = 0.2, TimeSpan? requestDelay = null, ILoggerFactory? loggerFactory = null)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _temperature = temperature;
            _requestDelay = requestDelay ?? TimeSpan.FromSeconds(7.5); // Default 7.5 seconds for 8 RPM
            _logger = loggerFactory?.CreateLogger<GeminiSubtitleTranslationService>() ?? NullLogger<GeminiSubtitleTranslationService>.Instance;
        }

        /// <inheritdoc />
        public async Task<SubtitleBatchResponse> TranslateBatchAsync(
            SubtitleBatch batch, Lang sourceLanguage, Lang targetLanguage)
        {
            if (batch.Lines == null || batch.Lines.Count == 0)
                throw new ArgumentException("Lines to translate must not be empty.", nameof(batch));

            _logger.LogInformation("Starting translation batch from {SourceLanguage} to {TargetLanguage} with {LineCount} lines and {ContextCount} context lines",
                sourceLanguage, targetLanguage, batch.Lines.Count, batch.Context.Count);

            // Map domain model directly to Gemini DTOs (string timestamps)
            var geminiRequest = new GeminiSubtitleBatchRequest(
                batch.Context.Select(GeminiSubtitleLineMapper.ToGeminiDto).ToList(),
                batch.Lines.Select(GeminiSubtitleLineMapper.ToGeminiDto).ToList()
            );

            // Use GeminiPromptFormatter for prompt and variables
            var promptFormatter = new GeminiPromptFormatter();
            var promptConfig = new PromptTemplateConfig(GeminiPromptFormatter.PromptTemplate)
            {
                InputVariables = new List<InputVariable>
                {
                    new() { Name = "sourceLanguage", Description = "Source language for translation" },
                    new() { Name = "targetLanguage", Description = "Target language for translation" },
                    new() { Name = "contextLinesFormatted", Description = "Formatted context lines as readable text" },
                    new() { Name = "linesToTranslateFormatted", Description = "Formatted lines to translate as readable text" }
                }
            };

            var variables = promptFormatter.FormatVariables(geminiRequest, sourceLanguage, targetLanguage);

            _logger.LogDebug("Context lines formatted: {ContextLines}", variables["contextLinesFormatted"]);
            _logger.LogDebug("Lines to translate formatted: {LinesToTranslate}", variables["linesToTranslateFormatted"]);

            var executionSettings = new Microsoft.SemanticKernel.Connectors.Google.GeminiPromptExecutionSettings
            {
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(GeminiSubtitleBatchResponse),
                Temperature = _temperature
            };

            // Create kernel arguments with template parameters
            var kernelArguments = new KernelArguments(executionSettings)
            {
                ["sourceLanguage"] = variables["sourceLanguage"],
                ["targetLanguage"] = variables["targetLanguage"],
                ["contextLinesFormatted"] = variables["contextLinesFormatted"],
                ["linesToTranslateFormatted"] = variables["linesToTranslateFormatted"]
            };

            // Polly retry with exponential backoff (unchanged)
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(30 * Math.Pow(2, attempt - 1)),
                    onRetry: (exception, timespan, attempt, context) =>
                    {
                        _logger.LogWarning("Retrying translation attempt {Attempt} after {DelaySeconds}s due to exception: {Exception}",
                            attempt, timespan.TotalSeconds, exception.Message);
                    });

            // Apply rate limiting to be polite to the Gemini API
            await ApplyRateLimitingAsync();

            var output = await retryPolicy.ExecuteAsync(async () =>
            {
                var promptFunction = _kernel.CreateFunctionFromPrompt(promptConfig);
                var result = await promptFunction.InvokeAsync(_kernel, kernelArguments);
                return result.ToString();
            });

            var geminiResponse = System.Text.Json.JsonSerializer.Deserialize<GeminiSubtitleBatchResponse>(output);
            if (geminiResponse?.TranslatedLines == null)
            {
                _logger.LogError("Translation failed: No translations received from API");
                throw new InvalidOperationException("No translations received from API.");
            }

            // Log the response - ToString() override will show beautiful JSON
            _logger.LogDebug("Gemini API response received: {Response}", geminiResponse);

            var expectedCount = batch.Lines.Count;
            var actualCount = geminiResponse.TranslatedLines.Count;

            if (actualCount != expectedCount)
            {
                _logger.LogError("Translation failed: Expected {ExpectedCount} translations, got {ActualCount}. " +
                    "Each input line must produce exactly one translation. API may have merged or skipped lines.",
                    expectedCount, actualCount);

                // Log the actual response for debugging
                _logger.LogDebug("Expected lines: {ExpectedLines}",
                    string.Join(", ", batch.Lines.Select(l => $"'{l.Text}'")));
                _logger.LogDebug("Received translations: {ReceivedTranslations}",
                    string.Join(", ", geminiResponse.TranslatedLines.Select(l => $"'{l.Text}' -> '{l.TranslatedText}'")));

                throw new InvalidOperationException($"Expected {expectedCount} translations, got {actualCount}. " +
                    "Each input line must produce exactly one translation.");
            }

            _logger.LogInformation("Translation batch completed successfully with {TranslatedCount} lines",
                geminiResponse.TranslatedLines.Count);

            // Map Gemini DTOs back to domain DTOs
            var response = new SubtitleBatchResponse
            {
                TranslatedLines = geminiResponse.TranslatedLines.ConvertAll(GeminiSubtitleLineMapper.FromGeminiDto)
            };
            return response;
        }

        /// <summary>
        /// Applies rate limiting to ensure we don't overwhelm the Gemini API with requests.
        /// Uses a semaphore to ensure only one request at a time and enforces a minimum delay between requests.
        /// </summary>
        private async Task ApplyRateLimitingAsync()
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _requestDelay)
                {
                    var delayNeeded = _requestDelay - timeSinceLastRequest;
                    _logger.LogDebug("Rate limiting: waiting {DelayMs}ms before next API request", delayNeeded.TotalMilliseconds);
                    await Task.Delay(delayNeeded);
                }
                _lastRequestTime = DateTime.UtcNow;
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }
    }
}
