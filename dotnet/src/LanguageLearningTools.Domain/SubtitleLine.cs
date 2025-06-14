using System;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Represents a single subtitle line with timing and text.
    /// </summary>
    public sealed class SubtitleLine
    {

        /// <summary>
        /// Gets the start time of the subtitle.
        /// </summary>
        public TimeSpan Start { get; }

        /// <summary>
        /// Gets the end time of the subtitle.
        /// </summary>
        public TimeSpan End { get; }

        /// <summary>
        /// Gets the text of the subtitle.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the translated text of the subtitle, if available.
        /// </summary>
        /// <remarks>
        /// This property is optional and may be null if no translation is present.
        /// </remarks>
        public string? TranslatedText { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleLine"/> class.
        /// </summary>
        /// <param name="start">Start time.</param>
        /// <param name="end">End time.</param>
        /// <param name="text">Subtitle text.</param>
        public SubtitleLine(TimeSpan start, TimeSpan end, string text)
        {
            Start = start;
            End = end;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            TranslatedText = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleLine"/> class with translation.
        /// </summary>
        /// <param name="start">Start time.</param>
        /// <param name="end">End time.</param>
        /// <param name="text">Subtitle text.</param>
        /// <param name="translatedText">Translated subtitle text (optional).</param>
        public SubtitleLine(TimeSpan start, TimeSpan end, string text, string? translatedText)
        {
            Start = start;
            End = end;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            TranslatedText = translatedText;
        }
    }
}
