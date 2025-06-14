using System.Collections.Generic;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Represents a batch translation request sent to the Gemini API.
    /// Contains context lines for better translation quality and the actual lines to translate.
    /// </summary>
    /// <remarks>
    /// <para><strong>Translation Context:</strong> Context lines help Gemini understand the conversation flow and provide more accurate translations.</para>
    /// <para><strong>Immutable Design:</strong> Once created, the request cannot be modified to ensure thread safety and prevent accidental changes.</para>
    /// </remarks>
    public sealed class GeminiSubtitleBatchRequest
    {
        /// <summary>
        /// Context lines that provide conversation history for better translation accuracy.
        /// These lines are not translated but help inform the translation of LinesToTranslate.
        /// </summary>
        public List<GeminiSubtitleLine> ContextLines { get; init; }
        
        /// <summary>
        /// The actual subtitle lines that need to be translated.
        /// Each line in this collection will have a corresponding translation in the response.
        /// </summary>
        public List<GeminiSubtitleLine> LinesToTranslate { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleBatchRequest"/> class.
        /// </summary>
        /// <param name="contextLines">Context lines for translation quality (can be empty)</param>
        /// <param name="linesToTranslate">Lines that need to be translated (must not be empty)</param>
        public GeminiSubtitleBatchRequest(List<GeminiSubtitleLine> contextLines, List<GeminiSubtitleLine> linesToTranslate)
        {
            ContextLines = contextLines ?? new();
            LinesToTranslate = linesToTranslate ?? new();
        }
    }
}
