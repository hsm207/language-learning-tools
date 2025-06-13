using System.Collections.Generic;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// DTO for Gemini API: batch response with string timestamps. Immutable.
    /// </summary>
    public sealed class GeminiSubtitleBatchResponse
    {
        public List<GeminiSubtitleLineDto> TranslatedLines { get; init; }

        public GeminiSubtitleBatchResponse(List<GeminiSubtitleLineDto> translatedLines)
        {
            TranslatedLines = translatedLines ?? new();
        }
    }
}
