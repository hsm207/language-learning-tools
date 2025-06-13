using System.Collections.Generic;
using System.Linq;
using LanguageLearningTools.Domain;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Responsible for formatting the Gemini prompt and variables for subtitle translation.
    /// </summary>
    /// <remarks>
    /// Encapsulates prompt template and variable formatting for maintainability and testability.
    /// </remarks>
    public class GeminiPromptFormatter
    {
        /// <summary>
        /// The prompt template for Gemini subtitle translation.
        /// </summary>
        public const string PromptTemplate = @"Translate the following subtitles from {{$sourceLanguage}} to {{$targetLanguage}}.
Use context if provided. Return only the translated lines in the required schema.

IMPORTANT:
- Preserve all emoji and special characters exactly as they appear in the input. Do not output Unicode codepoints, escape sequences, or names—output the actual emoji or special character.
- Do not add or remove any emoji or special characters.
- The output must be valid JSON matching the schema, with the TranslatedText containing the actual emoji or special character, not a description or codepoint.

{{$contextLinesFormatted}}

{{$linesToTranslateFormatted}}
";

        /// <summary>
        /// Formats the prompt variables for Gemini translation.
        /// </summary>
        /// <param name="request">The Gemini batch request.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>A dictionary of variables for the prompt.</returns>
        public Dictionary<string, object> FormatVariables(GeminiSubtitleBatchRequest request, Lang sourceLanguage, Lang targetLanguage)
        {
            var contextLinesFormatted = request.ContextLines.Count > 0
                ? "CONTEXT LINES (for reference):\n" + string.Join("\n", request.ContextLines.Select(line => $"[{line.Start} → {line.End}] {line.Text}"))
                : string.Empty;

            var linesToTranslateFormatted = "LINES TO TRANSLATE:\n" +
                string.Join("\n", request.LinesToTranslate.Select(line => $"[{line.Start} → {line.End}] {line.Text}"));

            return new Dictionary<string, object>
            {
                ["sourceLanguage"] = sourceLanguage.ToString(),
                ["targetLanguage"] = targetLanguage.ToString(),
                ["contextLinesFormatted"] = contextLinesFormatted,
                ["linesToTranslateFormatted"] = linesToTranslateFormatted
            };
        }
    }
}
