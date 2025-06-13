using System;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// DTO for Gemini API: subtitle line with string timestamps for Start/End. Immutable.
    /// </summary>
    public sealed class GeminiSubtitleLineDto
    {
        /// <summary>
        /// Start timestamp as string (hh:mm:ss.fff)
        /// </summary>
        public string Start { get; init; }
        /// <summary>
        /// End timestamp as string (hh:mm:ss.fff)
        /// </summary>
        public string End { get; init; }
        /// <summary>
        /// Subtitle text
        /// </summary>
        public string Text { get; init; }
        /// <summary>
        /// Translated subtitle text
        /// </summary>
        public string? TranslatedText { get; init; }

        public GeminiSubtitleLineDto(string start, string end, string text, string? translatedText)
        {
            Start = start;
            End = end;
            Text = text;
            TranslatedText = translatedText;
        }
    }
}
