using System;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Xunit;

namespace LanguageLearningTools.Infrastructure.Tests
{
    /// <summary>
    /// Unit tests for <see cref="GeminiSubtitleLineMapper"/>.
    /// </summary>
    public class GeminiSubtitleLineMapperTests
    {
        [Fact]
        public void ToGeminiDto_MapsCorrectly()
        {
            var line = new SubtitleLine(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Hello! 😊", "Hallo! 😊");
            var dto = GeminiSubtitleLineMapper.ToGeminiDto(line);
            Assert.Equal("Hello! 😊", dto.Text);
            Assert.Equal("Hallo! 😊", dto.TranslatedText);
        }

        [Fact]
        public void FromGeminiDto_MapsCorrectly()
        {
            var dto = new GeminiSubtitleLine("How are you?", "Wie geht's?");
            var line = GeminiSubtitleLineMapper.FromGeminiDto(dto);
            Assert.Equal(TimeSpan.Zero, line.Start);
            Assert.Equal(TimeSpan.Zero, line.End);
            Assert.Equal("How are you?", line.Text);
            Assert.Equal("Wie geht's?", line.TranslatedText);
        }

        [Fact]
        public void RoundTrip_DomainToDtoToDomain_PreservesData()
        {
            var original = new SubtitleLine(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(6), "Bye!", "Tschüss!");
            var dto = GeminiSubtitleLineMapper.ToGeminiDto(original);
            var roundTripped = GeminiSubtitleLineMapper.FromGeminiDto(dto);
            Assert.Equal(TimeSpan.Zero, roundTripped.Start);
            Assert.Equal(TimeSpan.Zero, roundTripped.End);
            Assert.Equal(original.Text, roundTripped.Text);
            Assert.Equal(original.TranslatedText, roundTripped.TranslatedText);
        }

        [Fact]
        public void ToGeminiDto_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GeminiSubtitleLineMapper.ToGeminiDto(null));
        }

        [Fact]
        public void FromGeminiDto_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GeminiSubtitleLineMapper.FromGeminiDto(null));
        }

        [Fact]
        public void FromGeminiDto_DecodesHtmlEntitiesInTranslatedText()
        {
            // HTML entities that Gemini sometimes returns instead of Unicode characters
            var dto = new GeminiSubtitleLine(
                "Hello world", 
                "&#129412; Regenb&ouml;gen sind nur Einhornschnupfen! &#127752; Caf&eacute; &amp; Spa&szlig;!");
            
            var line = GeminiSubtitleLineMapper.FromGeminiDto(dto);
            
            // Verify that HTML entities are decoded to proper Unicode characters
            Assert.Equal("🦄 Regenbögen sind nur Einhornschnupfen! 🌈 Café & Spaß!", line.TranslatedText);
            Assert.Equal("Hello world", line.Text); // Original text should remain unchanged
        }
    }
}
