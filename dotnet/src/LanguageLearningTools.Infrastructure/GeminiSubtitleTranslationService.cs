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

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleTranslationService"/> class.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance configured for Gemini.</param>
        /// <param name="temperature">The temperature for Gemini completions (0 = deterministic, 1 = most random). Default is 0.2.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.</param>
        public GeminiSubtitleTranslationService(Kernel kernel, double temperature = 0.2, ILoggerFactory? loggerFactory = null)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
            _temperature = temperature;
            _logger = loggerFactory?.CreateLogger<GeminiSubtitleTranslationService>() ?? NullLogger<GeminiSubtitleTranslationService>.Instance;
        }

        /// <inheritdoc />
        public async Task<SubtitleBatchResponse> TranslateBatchAsync(
            SubtitleBatchRequest request, Lang sourceLanguage, Lang targetLanguage)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.LinesToTranslate == null || request.LinesToTranslate.Count == 0)
                throw new ArgumentException("LinesToTranslate must not be empty.", nameof(request));

            _logger.LogInformation("Starting translation batch from {SourceLanguage} to {TargetLanguage} with {LineCount} lines and {ContextCount} context lines",
                sourceLanguage, targetLanguage, request.LinesToTranslate.Count, request.ContextLines.Count);

            // Map domain DTOs to Gemini DTOs (string timestamps)
            var geminiRequest = new GeminiSubtitleBatchRequest(
                request.ContextLines.ConvertAll(GeminiSubtitleLineMapper.ToGeminiDto),
                request.LinesToTranslate.ConvertAll(GeminiSubtitleLineMapper.ToGeminiDto)
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

            var output = await retryPolicy.ExecuteAsync(async () =>
            {
                var promptFunction = _kernel.CreateFunctionFromPrompt(promptConfig);
                var result = await promptFunction.InvokeAsync(_kernel, kernelArguments);
                var outStr = result.ToString();
                _logger.LogDebug("Gemini API response received: {Response}", outStr);
                return outStr;
            });

            var geminiResponse = System.Text.Json.JsonSerializer.Deserialize<GeminiSubtitleBatchResponse>(output);
            if (geminiResponse?.TranslatedLines == null || geminiResponse.TranslatedLines.Count != request.LinesToTranslate.Count)
            {
                _logger.LogError("Translation failed: Expected {ExpectedCount} translations, got {ActualCount}",
                    request.LinesToTranslate.Count, geminiResponse?.TranslatedLines?.Count ?? 0);
                throw new InvalidOperationException($"Expected {request.LinesToTranslate.Count} translations, got {geminiResponse?.TranslatedLines?.Count ?? 0}.");
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
    }
}
