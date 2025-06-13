using System.Collections.Generic;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// DTO for Gemini API: batch request with string timestamps. Immutable.
    /// </summary>
    public sealed class GeminiSubtitleBatchRequest
    {
        public List<GeminiSubtitleLineDto> ContextLines { get; init; }
        public List<GeminiSubtitleLineDto> LinesToTranslate { get; init; }

        public GeminiSubtitleBatchRequest(List<GeminiSubtitleLineDto> contextLines, List<GeminiSubtitleLineDto> linesToTranslate)
        {
            ContextLines = contextLines ?? new();
            LinesToTranslate = linesToTranslate ?? new();
        }
    }
}
