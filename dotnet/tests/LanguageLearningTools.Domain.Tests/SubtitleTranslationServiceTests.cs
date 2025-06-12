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
            var lines = new List<string> { "Hallo Welt", "Wie geht's?" };

            // Act
            var result = await service.TranslateBatchAsync(lines, Lang.German, Lang.English);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("Hallo Welt [English]", result[0]);
            Assert.Equal("Wie geht's? [English]", result[1]);
        }

        [Fact]
        public async Task TranslateBatchAsync_Should_Throw_On_Null_Lines()
        {
            var service = new DummySubtitleTranslationService();
            await Assert.ThrowsAsync<ArgumentNullException>(() => service.TranslateBatchAsync(null!, Lang.German, Lang.English));
        }

        [Fact]
        public async Task TranslateBatchAsync_Should_Throw_On_Empty_Lines()
        {
            var service = new DummySubtitleTranslationService();
            await Assert.ThrowsAsync<ArgumentException>(() => service.TranslateBatchAsync(new List<string>(), Lang.German, Lang.English));
        }

        // Dummy implementation for testing
        private class DummySubtitleTranslationService : ISubtitleTranslationService
        {
            public Task<IReadOnlyList<string>> TranslateBatchAsync(IReadOnlyList<string> lines, Lang sourceLanguage, Lang targetLanguage)
            {
                if (lines == null) throw new ArgumentNullException(nameof(lines));
                if (lines.Count == 0) throw new ArgumentException("Lines must not be empty.", nameof(lines));
                var translated = new List<string>();
                foreach (var line in lines)
                {
                    translated.Add($"{line} [{targetLanguage}]");
                }
                return Task.FromResult((IReadOnlyList<string>)translated);
            }
        }
    }
}
