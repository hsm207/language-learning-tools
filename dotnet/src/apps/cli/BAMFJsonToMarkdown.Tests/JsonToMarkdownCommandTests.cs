using BAMFJsonToMarkdown.Commands;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace BAMFJsonToMarkdown.Tests
{
    public class JsonToMarkdownCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_InputFileExists_ReturnsZero()
        {
            // Arrange
            var inputFilePath = "TestData/sample.json";
            var outputFilePath = "TestData/output.md";
            var expectedFilePath = "TestData/expected.md";
            var inputFile = new FileInfo(inputFilePath);
            var outputFile = new FileInfo(outputFilePath);
            var command = new JsonToMarkdownCommand(inputFile, outputFile);

            // Act
            var result = await command.ExecuteAsync();

            // Assert
            Assert.Equal(0, result);
            Assert.True(outputFile.Exists);
            var expectedContent = await File.ReadAllTextAsync(expectedFilePath);
            var actualContent = await File.ReadAllTextAsync(outputFilePath);
            Assert.Equal(expectedContent, actualContent);

            // Clean up
            if (outputFile.Exists)
            {
                outputFile.Delete();
            }
        }
    }
}
