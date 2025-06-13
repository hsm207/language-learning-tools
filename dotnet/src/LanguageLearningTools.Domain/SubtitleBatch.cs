using System.Collections.Generic;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Represents a batch of subtitle lines with context for translation.
    /// </summary>
    /// <param name="Context">The context lines that provide background for translation.</param>
    /// <param name="Lines">The subtitle lines to be translated in this batch.</param>
    public readonly record struct SubtitleBatch(
        IReadOnlyList<SubtitleLine> Context,
        IReadOnlyList<SubtitleLine> Lines);
}
