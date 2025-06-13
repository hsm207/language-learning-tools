using System;
using System.Collections.Generic;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Implements a rolling window batching strategy for subtitle translation.
    /// Each line is translated exactly once, with configurable context from previous lines.
    /// </summary>
    public class RollingWindowBatchingStrategy : ISubtitleBatchingStrategy
    {
        /// <inheritdoc />
        public IEnumerable<SubtitleBatch> CreateBatches(List<SubtitleLine> lines, int batchSize, int contextSize, int contextOverlap)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (contextSize < 0) throw new ArgumentOutOfRangeException(nameof(contextSize));
            if (contextOverlap < 0 || contextOverlap > batchSize) throw new ArgumentOutOfRangeException(nameof(contextOverlap));

            int currentIndex = 0;
            List<SubtitleLine> previousBatchForContext = new();

            while (currentIndex < lines.Count)
            {
                // Determine batch range
                int batchEnd = Math.Min(currentIndex + batchSize, lines.Count);
                var batch = lines.GetRange(currentIndex, batchEnd - currentIndex);

                // Determine context
                var context = new List<SubtitleLine>();
                
                if (contextSize > 0 && currentIndex > 0)
                {
                    // Calculate how many lines we can get from previous batch overlap
                    int availableOverlap = Math.Min(contextOverlap, previousBatchForContext.Count);
                    
                    // Calculate total context window size
                    int totalContextNeeded = Math.Min(contextSize, currentIndex);
                    
                    // Start from the position that gives us exactly contextSize lines (or all available)
                    int contextStart = currentIndex - totalContextNeeded;
                    context = lines.GetRange(contextStart, totalContextNeeded);
                }

                yield return new SubtitleBatch(context, batch);

                // Save current batch for next iteration's context
                previousBatchForContext = new List<SubtitleLine>(batch);
                currentIndex = batchEnd;
            }
        }
    }
}
