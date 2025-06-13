using System;
using System.Collections.Generic;
using System.Linq;
using LanguageLearningTools.Domain;
using Xunit;

namespace LanguageLearningTools.Domain.Tests
{
    public class RollingWindowBatchingStrategyTests
    {
        [Fact]
        public void CreateBatches_ReturnsExpectedBatches_WithContextOverlap()
        {
            var lines = Enumerable.Range(1, 7)
                .Select(i => new SubtitleLine(TimeSpan.FromSeconds(i), TimeSpan.FromSeconds(i + 1), $"Line {i}", null))
                .ToList();
            var strategy = new RollingWindowBatchingStrategy();
            var batches = strategy.CreateBatches(lines, batchSize: 3, contextSize: 2, contextOverlap: 1).ToList();

            // With 7 lines, batchSize=3, contextSize=2, contextOverlap=1:
            // Batch 1: Lines 1-3, no context
            // Batch 2: Lines 4-6, context: Line 3 (overlap) + Line 2 (additional)  
            // Batch 3: Line 7, context: Line 6 (overlap) + Line 5 (additional)
            Assert.Equal(3, batches.Count);
            
            Assert.Equal(new[] { "Line 1", "Line 2", "Line 3" }, batches[0].Lines.Select(l => l.Text));
            Assert.Empty(batches[0].Context);
            
            Assert.Equal(new[] { "Line 4", "Line 5", "Line 6" }, batches[1].Lines.Select(l => l.Text));
            Assert.Equal(new[] { "Line 2", "Line 3" }, batches[1].Context.Select(l => l.Text));
            
            Assert.Equal(new[] { "Line 7" }, batches[2].Lines.Select(l => l.Text));
            Assert.Equal(new[] { "Line 5", "Line 6" }, batches[2].Context.Select(l => l.Text));
        }

        [Fact]
        public void CreateBatches_ThrowsOnInvalidArgs()
        {
            var strategy = new RollingWindowBatchingStrategy();
            Assert.Throws<ArgumentNullException>(() => strategy.CreateBatches(null, 3, 2, 1).ToList());
            Assert.Throws<ArgumentOutOfRangeException>(() => strategy.CreateBatches(new List<SubtitleLine>(), 0, 2, 1).ToList());
            Assert.Throws<ArgumentOutOfRangeException>(() => strategy.CreateBatches(new List<SubtitleLine>(), 3, -1, 1).ToList());
            Assert.Throws<ArgumentOutOfRangeException>(() => strategy.CreateBatches(new List<SubtitleLine>(), 3, 2, -1).ToList());
            Assert.Throws<ArgumentOutOfRangeException>(() => strategy.CreateBatches(new List<SubtitleLine>(), 3, 2, 4).ToList());
        }
    }
}
