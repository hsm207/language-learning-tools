using System.Collections.Generic;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Strategy for batching subtitle lines with context for translation.
    /// </summary>
    public interface ISubtitleBatchingStrategy
    {
        /// <summary>
        /// Creates batches of subtitle lines with context for translation.
        /// Each line is translated exactly once, with context provided from previous lines.
        /// </summary>
        /// <param name="lines">All subtitle lines in the document.</param>
        /// <param name="batchSize">The number of lines to translate in each batch.</param>
        /// <param name="contextSize">The number of context lines to provide before each batch.</param>
        /// <param name="contextOverlap">The number of lines from the current batch to use as context for the next batch.</param>
        /// <returns>A sequence of subtitle batches where each line appears in exactly one batch.</returns>
        IEnumerable<SubtitleBatch> CreateBatches(List<SubtitleLine> lines, int batchSize, int contextSize, int contextOverlap);
    }
}
