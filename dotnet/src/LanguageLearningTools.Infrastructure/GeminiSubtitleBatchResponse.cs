using System.Collections.Generic;
using System.Text.Json;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Represents a batch translation response received from the Gemini API.
    /// Contains the translated subtitle lines corresponding to the original request.
    /// </summary>
    /// <remarks>
    /// <para><strong>Translation Results:</strong> Each line in TranslatedLines corresponds to a line from the original request's LinesToTranslate.</para>
    /// <para><strong>Quality Assurance:</strong> The count of TranslatedLines must exactly match the count of requested lines for translation.</para>
    /// <para><strong>Immutable Design:</strong> Response data cannot be modified after creation to maintain data integrity.</para>
    /// </remarks>
    public sealed class GeminiSubtitleBatchResponse
    {
        /// <summary>
        /// The translated subtitle lines returned by Gemini API.
        /// Each line contains both the original text and the translated result.
        /// The order and count must match the original LinesToTranslate from the request.
        /// </summary>
        public List<GeminiSubtitleLine> TranslatedLines { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeminiSubtitleBatchResponse"/> class.
        /// </summary>
        /// <param name="translatedLines">The translated subtitle lines from Gemini API</param>
        public GeminiSubtitleBatchResponse(List<GeminiSubtitleLine> translatedLines)
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
