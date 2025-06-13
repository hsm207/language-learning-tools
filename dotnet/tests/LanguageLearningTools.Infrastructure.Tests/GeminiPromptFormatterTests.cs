using System.Collections.Generic;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Xunit;

namespace LanguageLearningTools.Infrastructure.Tests
{
    /// <summary>
    /// Unit tests for <see cref="GeminiPromptFormatter"/>.
    /// </summary>
    public class GeminiPromptFormatterTests
    {
        [Fact]
        public void FormatVariables_WithContextLines_FormatsCorrectly()
        {
            // Arrange
            var request = new GeminiSubtitleBatchRequest(
                new List<GeminiSubtitleLineDto>
                {
                    new GeminiSubtitleLineDto("00:00:01.000", "00:00:02.000", "Hello! 😊", null)
                },
                new List<GeminiSubtitleLineDto>
                {
                    new GeminiSubtitleLineDto("00:00:03.000", "00:00:04.000", "How are you?", null)
                }
            );
            var formatter = new GeminiPromptFormatter();

            // Act
            var variables = formatter.FormatVariables(request, Lang.English, Lang.German);

            // Assert
            Assert.Equal("English", variables["sourceLanguage"]);
            Assert.Equal("German", variables["targetLanguage"]);
            Assert.Contains("CONTEXT LINES (for reference):", variables["contextLinesFormatted"].ToString());
            Assert.Contains("[00:00:01.000 → 00:00:02.000] Hello! 😊", variables["contextLinesFormatted"].ToString());
            Assert.Contains("LINES TO TRANSLATE:", variables["linesToTranslateFormatted"].ToString());
            Assert.Contains("[00:00:03.000 → 00:00:04.000] How are you?", variables["linesToTranslateFormatted"].ToString());
        }

        [Fact]
        public void FormatVariables_WithoutContextLines_FormatsCorrectly()
        {
            // Arrange
            var request = new GeminiSubtitleBatchRequest(
                new List<GeminiSubtitleLineDto>(),
                new List<GeminiSubtitleLineDto>
                {
                    new GeminiSubtitleLineDto("00:00:05.000", "00:00:06.000", "Goodbye!", null)
                }
            );
            var formatter = new GeminiPromptFormatter();

            // Act
            var variables = formatter.FormatVariables(request, Lang.English, Lang.German);

            // Assert
            Assert.Equal("English", variables["sourceLanguage"]);
            Assert.Equal("German", variables["targetLanguage"]);
            Assert.Equal(string.Empty, variables["contextLinesFormatted"]);
            Assert.Contains("LINES TO TRANSLATE:", variables["linesToTranslateFormatted"].ToString());
            Assert.Contains("[00:00:05.000 → 00:00:06.000] Goodbye!", variables["linesToTranslateFormatted"].ToString());
        }

        [Fact]
        public void FormatVariables_WithSpecialCharacters_PreservesCharacters()
        {
            // Arrange
            var request = new GeminiSubtitleBatchRequest(
                new List<GeminiSubtitleLineDto>(),
                new List<GeminiSubtitleLineDto>
                {
                    new GeminiSubtitleLineDto("00:00:07.000", "00:00:08.000", "¡Hola! ¿Qué tal? 🦄", null)
                }
            );
            var formatter = new GeminiPromptFormatter();

            // Act
            var variables = formatter.FormatVariables(request, Lang.German, Lang.English);

            // Assert
            Assert.Contains("¡Hola! ¿Qué tal? 🦄", variables["linesToTranslateFormatted"].ToString());
        }
    }
}
