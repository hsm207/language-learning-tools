using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Xunit;

namespace LanguageLearningTools.Infrastructure.Tests
{
    public class TtmlSubtitleParserTests
    {
        [Fact]
        public async Task ParseAsync_ValidTtml_ReturnsSubtitleLines()
        {
            // Use the sample TTML2 file with 3 lines for testing
            var filePath = "SampleThreeLine.ttml2";
            using var stream = File.OpenRead(filePath);
            var parser = new TtmlSubtitleParser();

            var lines = await parser.ParseAsync(stream);

            Assert.Equal(3, lines.Count);
            Assert.Equal(TimeSpan.Zero, lines[0].Start);
            Assert.Equal(TimeSpan.FromSeconds(1), lines[0].End);
            Assert.Contains("cat jumps", lines[0].Text);

            Assert.Equal(TimeSpan.FromSeconds(1.5), lines[1].Start);
            Assert.Equal(TimeSpan.FromSeconds(3), lines[1].End);
            Assert.Contains("unicorn sneezes", lines[1].Text);

            Assert.Equal(TimeSpan.FromSeconds(3.5), lines[2].Start);
            Assert.Equal(TimeSpan.FromSeconds(5), lines[2].End);
            Assert.Contains("circle of happiness", lines[2].Text);
        }
    }
}
