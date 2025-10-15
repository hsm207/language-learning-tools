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
        public async Task TranslateBatchAsync_WithMismatchedLineCount_UsesFallbackText()
        {
            // Arrange
            var mockGeminiKernelClient = new Mock<IGeminiKernelClient>();

            var response = new GeminiSubtitleBatchResponse(new List<GeminiSubtitleLine>
            {
                new GeminiSubtitleLine("Original Line 1", "Translated Line 1"),
                new GeminiSubtitleLine("Original Line 2", "Translated Line 2")
            });

            var jsonResponse = JsonSerializer.Serialize(response);

#pragma warning disable SKEXP0070
            mockGeminiKernelClient.Setup(c => c.InvokeGeminiAsync(It.IsAny<string>(), It.IsAny<GeminiPromptExecutionSettings>()))
#pragma warning restore SKEXP0070
                .ReturnsAsync(jsonResponse);

            var service = new GeminiSubtitleTranslationService(mockGeminiKernelClient.Object);

            var batch = new SubtitleBatch(new List<SubtitleLine>(), new List<SubtitleLine>
            {
                new SubtitleLine(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1), "Line 1"),
                new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Line 2"),
                new SubtitleLine(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3), "Line 3")
            });

            // Act
            var result = await service.TranslateBatchAsync(batch, Lang.English, Lang.German);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(batch.Lines.Count, result.TranslatedLines.Count);
            Assert.Equal("Translated Line 1", result.TranslatedLines[0].TranslatedText);
            Assert.Equal("Translated Line 2", result.TranslatedLines[1].TranslatedText);
            Assert.Equal("Line 3", result.TranslatedLines[2].TranslatedText); // Fallback to original text
        }
    }
}
