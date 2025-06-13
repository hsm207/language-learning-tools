using System;
using System.Collections.Generic;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// DTO for batching subtitle translation requests.
    /// </summary>
    public sealed class SubtitleBatchRequest
    {
        /// <summary>
        /// Lines providing context for the translation (e.g., previous/next lines), including timing and order.
        /// </summary>
        public List<SubtitleLine> ContextLines { get; set; } = new();

        /// <summary>
        /// The lines to be translated.
        /// </summary>
        public List<SubtitleLine> LinesToTranslate { get; set; } = new();
    }

    /// <summary>
    /// DTO for batching subtitle translation responses.
    /// </summary>
    public sealed class SubtitleBatchResponse
    {
        /// <summary>
        /// The translated lines (filled by the LLM).
        /// </summary>
        public List<SubtitleLine> TranslatedLines { get; set; } = new();
    }
}
