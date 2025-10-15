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
        public void CreatePromptArguments_WithContextLines_FormatsCorrectly()
        {
            // Arrange
            var contextLines = new List<SubtitleLine>
            {
                new SubtitleLine(System.TimeSpan.FromSeconds(1), System.TimeSpan.FromSeconds(2), "Hello! 😊")
            };
            var linesToTranslate = new List<SubtitleLine>
            {
                new SubtitleLine(System.TimeSpan.FromSeconds(3), System.TimeSpan.FromSeconds(4), "How are you?")
            };
            var formatter = new GeminiPromptFormatter();

            // Act
            var prompt = formatter.FormatPrompt(Lang.English, Lang.German, contextLines, linesToTranslate);

            // Assert
            Assert.Contains("Translate the following subtitles from English to German.", prompt);
            Assert.Contains("CONTEXT LINES (for reference):", prompt);
            Assert.Contains("[00:00:01.000 → 00:00:02.000] Hello! 😊", prompt);
            Assert.Contains("LINES TO TRANSLATE:", prompt);
            Assert.Contains("How are you?", prompt);
            Assert.DoesNotContain("[00:00:03.000 → 00:00:04.000]", prompt);
        }

        [Fact]
        public void CreatePromptArguments_WithoutContextLines_FormatsCorrectly()
        {
            // Arrange
            var contextLines = new List<SubtitleLine>();
            var linesToTranslate = new List<SubtitleLine>
            {
                new SubtitleLine(System.TimeSpan.FromSeconds(5), System.TimeSpan.FromSeconds(6), "Goodbye!")
            };
            var formatter = new GeminiPromptFormatter();

            // Act
            var prompt = formatter.FormatPrompt(Lang.English, Lang.German, contextLines, linesToTranslate);

            // Assert
            Assert.Contains("Translate the following subtitles from English to German.", prompt);
            Assert.DoesNotContain("CONTEXT LINES (for reference):", prompt);
            Assert.Contains("LINES TO TRANSLATE:", prompt);
            Assert.Contains("Goodbye!", prompt);
            Assert.DoesNotContain("[00:00:05.000 → 00:00:06.000]", prompt);
        }

        [Fact]
        public void CreatePromptArguments_WithSpecialCharacters_PreservesCharacters()
        {
            // Arrange
            var contextLines = new List<SubtitleLine>();
            var linesToTranslate = new List<SubtitleLine>
            {
                new SubtitleLine(System.TimeSpan.FromSeconds(7), System.TimeSpan.FromSeconds(8), "¡Hola! ¿Qué tal? 🦄")
            };
            var formatter = new GeminiPromptFormatter();

            // Act
            var prompt = formatter.FormatPrompt(Lang.German, Lang.English, contextLines, linesToTranslate);

            // Assert
            Assert.Contains("Translate the following subtitles from German to English.", prompt);
            Assert.Contains("LINES TO TRANSLATE:", prompt);
            Assert.Contains("¡Hola! ¿Qué tal? 🦄", prompt);
            Assert.DoesNotContain("[00:00:07.000 → 00:00:08.000]", prompt);
        }
    }
}
