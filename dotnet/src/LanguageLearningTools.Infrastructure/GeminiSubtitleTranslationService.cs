using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Translation service using Google Gemini via an AI service abstraction.
    /// </summary>
    public class GeminiSubtitleTranslationService : ISubtitleTranslationService
    {
        private readonly IGeminiKernelClient _geminiKernelClient;
        private readonly double _temperature;
        private readonly ILogger _logger;
        private readonly TimeSpan _requestDelay;
        private static readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
        private static DateTime _lastRequestTime = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleTranslationService"/> class.
        /// </summary>
        /// <param name="geminiKernelClient">The Gemini kernel client for AI interaction.</param>
        /// <param name="temperature">The temperature for Gemini completions (0 = deterministic, 1 = most random). Default is 0.2.</param>
        /// <param name="requestDelay">The minimum delay between API requests to avoid rate limiting. Default is 7.5 seconds for 8 RPM.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
        public GeminiSubtitleTranslationService(IGeminiKernelClient geminiKernelClient, double temperature = 0.2, TimeSpan? requestDelay = null, ILoggerFactory? loggerFactory = null)
        {
            _geminiKernelClient = geminiKernelClient ?? throw new ArgumentNullException(nameof(geminiKernelClient));
            _temperature = temperature;
            _requestDelay = requestDelay ?? TimeSpan.FromSeconds(7.5); // Default 7.5 seconds for 8 RPM
            _logger = loggerFactory?.CreateLogger<GeminiSubtitleTranslationService>() ?? NullLogger<GeminiSubtitleTranslationService>.Instance;
        }

        /// <inheritdoc />
        public async Task<SubtitleBatchResponse> TranslateBatchAsync(
            SubtitleBatch batch, Lang sourceLanguage, Lang targetLanguage)
        {
            _logger.LogInformation("Starting translation batch from {SourceLanguage} to {TargetLanguage} with {LineCount} lines and {ContextCount} context lines",
                sourceLanguage, targetLanguage, batch.Lines.Count, batch.Context.Count);

            // 1. Validate input batch
            ValidateBatchInput(batch);

            // 2. Format the prompt for Gemini
            var prompt = FormatGeminiPrompt(sourceLanguage, targetLanguage, batch.Context, batch.Lines);

            try
            {
                // 3. Execute Gemini translation with retry and rate limiting
                var output = await ExecuteGeminiTranslationWithRetryAndRateLimit(prompt);

                // 4. Process Gemini's response and map to domain models
                return ProcessGeminiResponse(batch, output);
            }
            catch (TranslationLineCountMismatchException ex)
            {
                return HandleTranslationMismatchFallback(batch, ex);
            }
        }

        private SubtitleBatchResponse HandleTranslationMismatchFallback(SubtitleBatch batch, TranslationLineCountMismatchException ex)
        {
            _logger.LogError("Translation failed after all retries due to line count mismatch. Returning batch with blank translations. Details: {0}", ex.Message);
            
            var blankTranslatedLines = new List<SubtitleLine>();
            for (int i = 0; i < batch.Lines.Count; i++)
            {
                var originalLine = batch.Lines[i];
                // Log specific missing lines for better debugging
                if (i >= ex.ActualCount)
                {
                    _logger.LogWarning("Missing translation for line {LineNumber} (Time: {StartTime}-{EndTime}, Text: \"{OriginalText}\"). Using blank text as fallback.",
                        i + 1, originalLine.Start, originalLine.End, originalLine.Text);
                }
                // Blank out all translated text for this failed batch
                blankTranslatedLines.Add(new SubtitleLine(
                    originalLine.Start,
                    originalLine.End,
                    originalLine.Text,
                    string.Empty // Blank translated text
                ));
            }
            return new SubtitleBatchResponse
            {
                TranslatedLines = blankTranslatedLines
            };
        }

        private void ValidateBatchInput(SubtitleBatch batch)
        {
            if (batch.Lines == null || batch.Lines.Count == 0)
                throw new ArgumentException("Lines to translate must not be empty.", nameof(batch));
        }

        private string FormatGeminiPrompt(Lang sourceLanguage, Lang targetLanguage, IReadOnlyList<SubtitleLine> contextLines, IReadOnlyList<SubtitleLine> linesToTranslate)
        {
            var promptFormatter = new GeminiPromptFormatter();
            return promptFormatter.FormatPrompt(sourceLanguage, targetLanguage, contextLines, linesToTranslate);
        }

        private async Task<string> ExecuteGeminiTranslationWithRetryAndRateLimit(string prompt)
        {
            var retryPolicy = CreateRetryPolicy();

            await ApplyRateLimitingAsync();

            return await retryPolicy.ExecuteAsync(async () =>
            {
                var executionSettings = new GeminiPromptExecutionSettings
                {
                    Temperature = _temperature,
                    ResponseMimeType = "application/json",
                    ResponseSchema = typeof(GeminiSubtitleBatchResponse)
                };

                return await _geminiKernelClient.InvokeGeminiAsync(prompt, executionSettings);
            });
        }

        private AsyncRetryPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<HttpRequestException>()
                .Or<TranslationLineCountMismatchException>() // Explicitly handle this custom exception
                .Or<InvalidOperationException>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1) * 5),
                    onRetry: (exception, timespan, attempt, context) =>
                    {
                        _logger.LogWarning("Retrying translation attempt {Attempt} after {DelaySeconds:N1}s due to exception: {ErrorMessage}",
                            attempt, timespan.TotalSeconds, exception.Message);
                    });
        }

        private SubtitleBatchResponse ProcessGeminiResponse(SubtitleBatch batch, string geminiOutput)
        {
            var geminiResponse = System.Text.Json.JsonSerializer.Deserialize<GeminiSubtitleBatchResponse>(geminiOutput);
            if (geminiResponse?.TranslatedLines == null)
            {
                _logger.LogError("Translation failed: Gemini response contained no translated lines.");
                throw new TranslationLineCountMismatchException(batch.Lines.Count, 0);
            }

            if (geminiResponse.TranslatedLines.Count != batch.Lines.Count)
            {
                _logger.LogWarning("Translation discrepancy: Expected {ExpectedCount} translations, got {ActualCount}. This will trigger a retry.",
                    batch.Lines.Count, geminiResponse.TranslatedLines.Count);
                throw new TranslationLineCountMismatchException(batch.Lines.Count, geminiResponse.TranslatedLines.Count);
            }

            _logger.LogInformation("Translation batch completed successfully with {TranslatedCount} lines",
                geminiResponse.TranslatedLines.Count);

            var translatedSubtitleLines = new List<SubtitleLine>();
            for (int i = 0; i < batch.Lines.Count; i++)
            {
                var originalLine = batch.Lines[i];
                var translatedGeminiLine = geminiResponse.TranslatedLines[i];
                translatedSubtitleLines.Add(new SubtitleLine(
                    originalLine.Start,
                    originalLine.End,
                    originalLine.Text,
                    WebUtility.HtmlDecode(translatedGeminiLine.TranslatedText) // Decode HTML entities
                ));
            }

            return new SubtitleBatchResponse
            {
                TranslatedLines = translatedSubtitleLines
            };
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
