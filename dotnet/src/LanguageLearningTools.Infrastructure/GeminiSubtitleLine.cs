using System;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Represents a subtitle line for Gemini API communication with string-based timestamps.
    /// Used in both request and response scenarios for subtitle translation.
    /// </summary>
    /// <remarks>
    /// <para><strong>Usage in Translation Flow:</strong></para>
    /// <list type="bullet">
    /// <item><description><strong>Request Phase:</strong> Contains original text to be translated (TranslatedText may be null/empty)</description></item>
    /// <item><description><strong>Response Phase:</strong> Contains both original text and the translated result</description></item>
    /// </list>
    /// <para><strong>String Timestamps:</strong> Uses "hh:mm:ss.fff" format for JSON serialization compatibility with Gemini API</para>
    /// </remarks>
    public sealed class GeminiSubtitleLine
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

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleLine"/> class.
        /// </summary>
        /// <param name="start">Start timestamp in "hh:mm:ss.fff" format</param>
        /// <param name="end">End timestamp in "hh:mm:ss.fff" format</param>
        /// <param name="text">Original subtitle text</param>
        /// <param name="translatedText">Translated text (null/empty in requests, populated in responses)</param>
        public GeminiSubtitleLine(string start, string end, string text, string? translatedText)
        {
            Start = start;
            End = end;
            Text = text;
            TranslatedText = translatedText;
        }
    }
}
