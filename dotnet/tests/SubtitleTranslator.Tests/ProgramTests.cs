using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageLearningTools.Domain;
using Xunit;

namespace SubtitleTranslator.Tests
{
    /// <summary>
    /// Tests for the SubtitleTranslator CLI Program class.
    /// </summary>
    public class ProgramTests
    {
        /// <summary>
        /// Tests that GenerateOutputFileName creates a proper filename with '_translated.json' suffix.
        /// </summary>
        [Fact]
        public void GenerateOutputFileName_ShouldAddTranslatedJsonSuffix()
        {
            // Arrange
            var inputFile = new FileInfo("/path/to/subtitle.ttml");

            // Act
            var result = Program.GenerateOutputFileName(inputFile);

            // Assert
            result.Name.Should().Be("subtitle_translated.json");
            result.DirectoryName.Should().Be("/path/to");
        }

        /// <summary>
        /// Tests that GenerateOutputFileName handles files without extensions properly.
        /// </summary>
        [Fact]
        public void GenerateOutputFileName_WithoutExtension_ShouldAddTranslatedJsonSuffix()
        {
            // Arrange
            var inputFile = new FileInfo("/path/to/subtitle");

            // Act
            var result = Program.GenerateOutputFileName(inputFile);

            // Assert
            result.Name.Should().Be("subtitle_translated.json");
            result.DirectoryName.Should().Be("/path/to");
        }

        /// <summary>
        /// Tests that the program can parse valid language codes correctly.
        /// </summary>
        [Theory]
        [InlineData("de", Lang.German)]
        [InlineData("en", Lang.English)]
        [InlineData("german", Lang.German)]
        [InlineData("english", Lang.English)]
        public void TryParseFromCode_WithValidCodes_ShouldReturnCorrectLang(string code, Lang expected)
        {
            // Act
            var success = LangExtensions.TryParseFromCode(code, out var result);

            // Assert
            success.Should().BeTrue();
            result.Should().Be(expected);
        }

        /// <summary>
        /// Tests that the program rejects invalid language codes.
        /// </summary>
        [Theory]
        [InlineData("fr")]
        [InlineData("spanish")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParseFromCode_WithInvalidCodes_ShouldReturnFalse(string? code)
        {
            // Act
            var success = LangExtensions.TryParseFromCode(code, out var result);

            // Assert
            success.Should().BeFalse();
            result.Should().Be(default(Lang));
        }

        /// <summary>
        /// Tests that language display names are correct.
        /// </summary>
        [Theory]
        [InlineData(Lang.German, "German")]
        [InlineData(Lang.English, "English")]
        public void GetDisplayName_ShouldReturnCorrectDisplayName(Lang lang, string expected)
        {
            // Act
            var result = lang.GetDisplayName();

            // Assert
            result.Should().Be(expected);
        }

        /// <summary>
        /// Tests that language codes are correct.
        /// </summary>
        [Theory]
        [InlineData(Lang.German, "de")]
        [InlineData(Lang.English, "en")]
        public void ToCode_ShouldReturnCorrectCode(Lang lang, string expected)
        {
            // Act
            var result = lang.ToCode();

            // Assert
            result.Should().Be(expected);
        }
    }
}