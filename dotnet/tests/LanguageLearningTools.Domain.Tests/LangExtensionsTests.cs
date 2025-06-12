using System;
using LanguageLearningTools.Domain;
using Xunit;

namespace LanguageLearningTools.Domain.Tests
{
    public class LangExtensionsTests
    {
        [Theory]
        [InlineData(Lang.German, "de")]
        [InlineData(Lang.English, "en")]
        public void ToCode_Returns_Correct_Language_Code(Lang lang, string expectedCode)
        {
            Assert.Equal(expectedCode, lang.ToCode());
        }

        [Fact]
        public void ToCode_Throws_On_Unknown_Lang()
        {
            // Simulate an invalid enum value
            Lang invalid = (Lang)999;
            Assert.Throws<ArgumentOutOfRangeException>(() => invalid.ToCode());
        }
    }
}
