using System;
using System.Collections.Generic;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// DTO for batching subtitle translation responses.
    /// </summary>
    public sealed class SubtitleBatchResponse
    {
        /// <summary>
        /// The translated lines with their original context.
        /// </summary>
        public List<SubtitleLine> TranslatedLines { get; set; } = new();
    }
}
