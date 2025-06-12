using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace SubtitleToJson.Tests;

public class ProgramTests
{
    [Fact]
    public async Task ConvertCommand_Should_Require_InputOption()
    {
        // Arrange
        var args = new[] { "convert" };
        // Act
        var exitCode = await SubtitleToJson.Program.Main(args);
        // Assert
        Assert.NotEqual(0, exitCode); // Should fail due to missing required --input
    }

    // ...existing code...

    [Fact]
    public async Task ConvertCommand_Should_Produce_Expected_Json_For_SampleTtml()
    {
        // Arrange
        var inputPath = Path.GetFullPath("SampleThreeLine.ttml2");
        var outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        if (File.Exists(outputPath)) File.Delete(outputPath); // Ensure it does not exist
        var args = new[] { "--input", inputPath, "--output", outputPath, "--format", "ttml" };

        // Act
        var exitCode = await SubtitleToJson.Program.Main(args);

        // Assert
        Assert.Equal(0, exitCode); // Should succeed
        Assert.True(File.Exists(outputPath), "Output file should exist");
        var json = await File.ReadAllTextAsync(outputPath);
        // This is a placeholder for the expected output structure
        // You should update this to match your actual expected JSON
        Assert.Contains("The cat jumps over the moon", json);
        Assert.Contains("Rainbows are just unicorn sneezes", json);
        Assert.Contains("Pizza is a circle of happiness", json);
    }
}
