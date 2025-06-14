using System.Collections.Generic;
using System.Text.Json;

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

        /// <summary>
        /// Returns a beautiful JSON representation of this response.
        /// </summary>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
