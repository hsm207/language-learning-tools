using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Application;
using Moq;
using Xunit;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LanguageLearningTools.Infrastructure.Tests
{
    /// <summary>
    /// Unit tests for GeminiSubtitleTranslationService, focusing on internal logic and retry behavior.
    /// </summary>
    public class GeminiSubtitleTranslationServiceTests
    {
        [Fact]
        public async Task TranslateBatchAsync_WithMismatchedLineCount_ThrowsTranslationLineCountMismatchException()
        {
            // Arrange
            var mockGeminiKernelClient = new Mock<IGeminiKernelClient>();

            var response = new GeminiSubtitleBatchResponse(new List<GeminiSubtitleLine>
            {
                new GeminiSubtitleLine("Original Line 1", "Translated Line 1"),
                new GeminiSubtitleLine("Original Line 2", "Translated Line 2")
            });

            var jsonResponse = JsonSerializer.Serialize(response);

            mockGeminiKernelClient.Setup(c => c.InvokeGeminiAsync(It.IsAny<string>(), It.IsAny<GeminiPromptExecutionSettings>()))
                .ReturnsAsync(jsonResponse);

            var service = new GeminiSubtitleTranslationService(mockGeminiKernelClient.Object);

            var batch = new SubtitleBatch(new List<SubtitleLine>(), new List<SubtitleLine>
            {
                new SubtitleLine(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), "Line 1")
            });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.TranslateBatchAsync(batch, Lang.English, Lang.German));

            Assert.Contains("Expected 1 translations, got 2.", exception.Message);
        }
    }
}
