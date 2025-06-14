using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LanguageLearningTools.Application;
using LanguageLearningTools.Domain;
using Moq;
using Xunit;

namespace LanguageLearningTools.Application.Tests
{
    /// <summary>
    /// Unit tests for the SubtitleTranslationApplicationService.
    /// 
    /// These tests verify that the application service correctly orchestrates the translation workflow:
    /// 1. Takes a SubtitleDocument and breaks it into batches using the configured strategy
    /// 2. Translates each batch using the translation service
    /// 3. Combines the results back into a new SubtitleDocument with both original and translated text
    /// 
    /// The service acts as a coordinator between domain models and infrastructure services,
    /// ensuring proper separation of concerns while maintaining testability through dependency injection.
    /// </summary>
    public class SubtitleTranslationApplicationServiceTests
    {
        private readonly Mock<ISubtitleTranslationService> _mockTranslationService;
        private readonly Mock<ISubtitleBatchingStrategy> _mockBatchingStrategy;
        private readonly Mock<ISubtitleParser> _mockSubtitleParser;
        private readonly SubtitleTranslationApplicationService _service;

        public SubtitleTranslationApplicationServiceTests()
        {
            _mockTranslationService = new Mock<ISubtitleTranslationService>();
            _mockBatchingStrategy = new Mock<ISubtitleBatchingStrategy>();
            _mockSubtitleParser = new Mock<ISubtitleParser>();
            _service = new SubtitleTranslationApplicationService(
                _mockTranslationService.Object,
                _mockBatchingStrategy.Object,
                _mockSubtitleParser.Object);
        }

        [Fact]
        public async Task TranslateDocumentAsync_WithValidDocument_ReturnsTranslatedDocument()
        {
            // This test verifies the happy path: the service takes a document with subtitle lines,
            // uses the batching strategy to group them, translates each batch, and returns
            // a new document where each line has both original and translated text.

            // Arrange - Set up test data with German subtitle lines
            var originalLines = new[]
            {
                new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), "Hallo Welt"),
                new SubtitleLine(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), "Wie geht es dir?")
            };
            var document = new SubtitleDocument(originalLines);

            // Mock the batching strategy to return a single batch containing all lines
            var batch = new SubtitleBatch(Array.Empty<SubtitleLine>(), originalLines);
            var batches = new[] { batch };

            // Mock the translation service to return translated versions of the lines
            var translatedLines = new[]
            {
                new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), "Hallo Welt", "Hello World"),
                new SubtitleLine(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), "Wie geht es dir?", "How are you?")
            };
            var batchResponse = new SubtitleBatchResponse { TranslatedLines = translatedLines.ToList() };

            // Configure mocks to return our test data
            _mockBatchingStrategy
                .Setup(x => x.CreateBatches(It.IsAny<List<SubtitleLine>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(batches);

            _mockTranslationService
                .Setup(x => x.TranslateBatchAsync(It.IsAny<SubtitleBatchRequest>(), Lang.German, Lang.English))
                .ReturnsAsync(batchResponse);

            // Act - Call the service method we're testing
            var result = await _service.TranslateDocumentAsync(document, Lang.German, Lang.English);

            // Assert - Verify the service returns a document with properly translated lines
            Assert.NotNull(result);
            Assert.Equal(2, result.Lines.Count);
            Assert.Equal("Hello World", result.Lines[0].TranslatedText);
            Assert.Equal("How are you?", result.Lines[1].TranslatedText);
            // Verify original text is preserved
            Assert.Equal("Hallo Welt", result.Lines[0].Text);
            Assert.Equal("Wie geht es dir?", result.Lines[1].Text);
        }

        [Fact]
        public async Task TranslateDocumentAsync_WithNullDocument_ThrowsArgumentNullException()
        {
            // This test ensures the service validates its input parameters.
            // A null document should result in an ArgumentNullException to provide
            // clear feedback about invalid usage rather than obscure null reference errors.

            // Act & Assert - Verify proper exception is thrown for invalid input
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.TranslateDocumentAsync(null!, Lang.German, Lang.English));
        }

        [Fact]
        public async Task TranslateDocumentAsync_WithEmptyDocument_ReturnsEmptyDocument()
        {
            // This test verifies the service handles edge cases gracefully.
            // An empty document (no subtitle lines) should return an empty document
            // without attempting any translation calls, avoiding unnecessary API costs
            // and potential errors from translation services.

            // Arrange - Create an empty document with no subtitle lines
            var emptyDocument = new SubtitleDocument(Array.Empty<SubtitleLine>());
            var emptyBatches = Array.Empty<SubtitleBatch>();

            // Mock the batching strategy to return no batches for empty input
            _mockBatchingStrategy
                .Setup(x => x.CreateBatches(It.IsAny<List<SubtitleLine>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(emptyBatches);

            // Act - Process the empty document
            var result = await _service.TranslateDocumentAsync(emptyDocument, Lang.German, Lang.English);

            // Assert - Verify we get an empty document back without errors
            Assert.NotNull(result);
            Assert.Empty(result.Lines);

            // Verify that no translation service calls were made (efficiency check)
            _mockTranslationService.Verify(
                x => x.TranslateBatchAsync(It.IsAny<SubtitleBatchRequest>(), It.IsAny<Lang>(), It.IsAny<Lang>()),
                Times.Never);
        }

        [Fact]
        public async Task TranslateDocumentAsync_WithMultipleBatches_CombinesResultsCorrectly()
        {
            // This test verifies the service correctly handles the batching workflow:
            // 1. Large documents may be split into multiple batches for translation
            // 2. Each batch is translated separately (potentially for context/API limits)
            // 3. Results from all batches are combined back into a single document
            // 4. The final document maintains the original order of subtitle lines

            // Arrange - Create a document with 5 lines that will be split into 2 batches
            var lines = Enumerable.Range(1, 5)
                .Select(i => new SubtitleLine(TimeSpan.FromSeconds(i), TimeSpan.FromSeconds(i + 1), $"German text {i}"))
                .ToArray();
            var document = new SubtitleDocument(lines);

            // Mock batching strategy to split into 2 batches: first 3 lines, then remaining 2
            var batch1 = new SubtitleBatch(Array.Empty<SubtitleLine>(), lines.Take(3).ToArray());
            var batch2 = new SubtitleBatch(Array.Empty<SubtitleLine>(), lines.Skip(3).ToArray());
            var batches = new[] { batch1, batch2 };

            // Mock translation responses for each batch
            var translatedLines1 = lines.Take(3)
                .Select((line, i) => new SubtitleLine(line.Start, line.End, line.Text, $"English text {i + 1}"))
                .ToArray();
            var translatedLines2 = lines.Skip(3)
                .Select((line, i) => new SubtitleLine(line.Start, line.End, line.Text, $"English text {i + 4}"))
                .ToArray();

            // Configure mocks to return our test batches and translations
            _mockBatchingStrategy
                .Setup(x => x.CreateBatches(It.IsAny<List<SubtitleLine>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns(batches);

            // Use SetupSequence to return different responses for each batch translation call
            _mockTranslationService
                .SetupSequence(x => x.TranslateBatchAsync(It.IsAny<SubtitleBatchRequest>(), Lang.German, Lang.English))
                .ReturnsAsync(new SubtitleBatchResponse { TranslatedLines = translatedLines1.ToList() })
                .ReturnsAsync(new SubtitleBatchResponse { TranslatedLines = translatedLines2.ToList() });

            // Act - Process the document that requires multiple batches
            var result = await _service.TranslateDocumentAsync(document, Lang.German, Lang.English);

            // Assert - Verify all lines are present and correctly translated
            Assert.Equal(5, result.Lines.Count);
            Assert.All(result.Lines, line => Assert.NotNull(line.TranslatedText));

            // Verify specific translations to ensure correct batch combination
            Assert.Contains("English text 1", result.Lines.Select(l => l.TranslatedText));
            Assert.Contains("English text 5", result.Lines.Select(l => l.TranslatedText));

            // Verify that translation service was called exactly twice (once per batch)
            _mockTranslationService.Verify(
                x => x.TranslateBatchAsync(It.IsAny<SubtitleBatchRequest>(), Lang.German, Lang.English),
                Times.Exactly(2));
        }
    }
}
