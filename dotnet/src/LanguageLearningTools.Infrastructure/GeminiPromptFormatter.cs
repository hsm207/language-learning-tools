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

CRITICAL REQUIREMENTS:
- You MUST translate EXACTLY the same number of lines as provided in ""LINES TO TRANSLATE"" section
- Each input line MUST correspond to exactly ONE output translation
- Do NOT merge, combine, or split lines under any circumstances
- Do NOT skip any lines, even if they seem incomplete or related to other lines
- Preserve all emoji and special characters exactly as they appear in the input. Do not output Unicode codepoints, escape sequences, or names—output the actual emoji or special character.
- Do not add or remove any emoji or special characters.
- The output must be valid JSON matching the schema, with the TranslatedText containing the actual emoji or special character, not a description or codepoint.
- If a line seems incomplete or cut off, translate it as-is without trying to complete it or merge it with other lines.

{{$contextLinesFormatted}}

{{$linesToTranslateFormatted}}";

        /// <summary>
        /// Formats the prompt variables for Gemini translation.
        /// </summary>
        /// <param name="request">The Gemini batch request.</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>A dictionary of variables for the prompt.</returns>
        public string FormatPrompt(Lang sourceLanguage, Lang targetLanguage, IReadOnlyList<SubtitleLine> contextLines, IReadOnlyList<SubtitleLine> linesToTranslate)
        {
            var contextLinesFormatted = contextLines.Count > 0
                ? "CONTEXT LINES (for reference):\n" + string.Join("\n", contextLines.Select(line => $"[{line.Start:hh\\:mm\\:ss\\.fff} → {line.End:hh\\:mm\\:ss\\.fff}] {line.Text}"))
                : string.Empty;

            var linesToTranslateFormatted = "LINES TO TRANSLATE:\n" +
                string.Join("\n", linesToTranslate.Select(line => line.Text));

            return PromptTemplate
                .Replace("{{$sourceLanguage}}", sourceLanguage.ToString())
                .Replace("{{$targetLanguage}}", targetLanguage.ToString())
                .Replace("{{$contextLinesFormatted}}", contextLinesFormatted)
                .Replace("{{$linesToTranslateFormatted}}", linesToTranslateFormatted);
        }
    }
}
