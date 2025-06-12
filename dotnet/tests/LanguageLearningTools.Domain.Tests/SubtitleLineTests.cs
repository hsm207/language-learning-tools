using System;
using LanguageLearningTools.Domain;
using Xunit;

namespace LanguageLearningTools.Domain.Tests
{
    public class SubtitleLineTests
    {
        [Fact]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var start = TimeSpan.FromSeconds(1);
            var end = TimeSpan.FromSeconds(2);
            var text = "Hello!";

            var line = new SubtitleLine(start, end, text);

            Assert.Equal(start, line.Start);
            Assert.Equal(end, line.End);
            Assert.Equal(text, line.Text);
        }

        [Fact]
        public void Constructor_ThrowsOnNullText()
        {
            Assert.Throws<ArgumentNullException>(() => new SubtitleLine(TimeSpan.Zero, TimeSpan.Zero, null));
        }
    }
}
