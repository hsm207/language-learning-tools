using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using Xunit;

namespace LanguageLearningTools.Domain.Tests
{
    public class SubtitleTranslationServiceTests
    {
        [Fact]
        public async Task TranslateBatchAsync_Should_Return_Translated_Texts_In_Order()
        {
            // Arrange
            var service = new DummySubtitleTranslationService();
            var lines = new List<SubtitleLine>
            {
                new SubtitleLine(TimeSpan.Zero, TimeSpan.Zero, "Hallo Welt"),
                new SubtitleLine(TimeSpan.Zero, TimeSpan.Zero, "Wie geht's?")
            };
            var request = new SubtitleBatchRequest
            {
                ContextLines = new List<SubtitleLine>(),
                LinesToTranslate = lines
            };

            // Act
            var result = await service.TranslateBatchAsync(request, Lang.German, Lang.English);

            // Assert
            Assert.Equal(2, result.TranslatedLines.Count);
            Assert.Equal("Hallo Welt [English]", result.TranslatedLines[0].TranslatedText);
            Assert.Equal("Wie geht's? [English]", result.TranslatedLines[1].TranslatedText);
        }

        [Fact]
        public async Task TranslateBatchAsync_Should_Throw_On_Null_Request()
        {
            var service = new DummySubtitleTranslationService();
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.TranslateBatchAsync(null!, Lang.German, Lang.English));
        }

        [Fact]
        public async Task TranslateBatchAsync_Should_Throw_On_Empty_Lines()
        {
            var service = new DummySubtitleTranslationService();
            var request = new SubtitleBatchRequest { ContextLines = new List<SubtitleLine>(), LinesToTranslate = new List<SubtitleLine>() };
            await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateBatchAsync(request, Lang.German, Lang.English));
        }

        // Dummy implementation for testing
        private class DummySubtitleTranslationService : ISubtitleTranslationService
        {
            public Task<SubtitleBatchResponse> TranslateBatchAsync(SubtitleBatchRequest request, Lang sourceLanguage, Lang targetLanguage)
            {
                if (request == null) throw new ArgumentNullException(nameof(request));
                if (request.LinesToTranslate == null || request.LinesToTranslate.Count == 0)
                    throw new ArgumentException("LinesToTranslate must not be empty.", nameof(request));
                var translated = new List<SubtitleLine>();
                foreach (var line in request.LinesToTranslate)
                {
                    translated.Add(new SubtitleLine(line.Start, line.End, line.Text, $"{line.Text} [{targetLanguage}]") );
                }
                return Task.FromResult(new SubtitleBatchResponse { TranslatedLines = translated });
            }
        }
    }
}
