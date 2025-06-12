using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using Microsoft.SemanticKernel;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Translation service using Google Gemini via Semantic Kernel.
    /// </summary>
    public class GeminiSubtitleTranslationService : ISubtitleTranslationService
    {
        private readonly Kernel _kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleTranslationService"/> class.
        /// </summary>
        /// <param name="kernel">The Semantic Kernel instance configured for Gemini.</param>
        public GeminiSubtitleTranslationService(Kernel kernel)
        {
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> TranslateBatchAsync(
            IReadOnlyList<string> lines, Lang sourceLanguage, Lang targetLanguage)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (lines.Count == 0) throw new ArgumentException("Lines must not be empty.", nameof(lines));

            // Build a prompt for Gemini
            var prompt = $"Translate the following lines from {sourceLanguage.ToCode()} to {targetLanguage.ToCode()} as a numbered list, one translation per line. Only output the translations, no extra text.\n";
            for (int i = 0; i < lines.Count; i++)
            {
                prompt += $"{i + 1}. {lines[i]}\n";
            }

            // Call Gemini via Semantic Kernel
            var result = await _kernel.InvokePromptAsync(prompt);
            var output = result.ToString();

            // Parse the output: expect a numbered list
            var translations = output
                .Split('\n')
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line =>
                {
                    var idx = line.IndexOf('.');
                    return idx > 0 ? line[(idx + 1)..].Trim() : line;
                })
                .ToList();

            if (translations.Count != lines.Count)
                throw new InvalidOperationException($"Expected {lines.Count} translations, got {translations.Count}.");

            return translations;
        }
    }
}
