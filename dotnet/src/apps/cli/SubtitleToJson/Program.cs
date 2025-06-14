using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using SubtitleToJson.Services;

namespace SubtitleToJson;

/// <summary>
/// Entry point for the SubtitleToJson CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point. Sets up the CLI commands and options.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        var inputOption = new Option<FileInfo?>(
            name: "--input",
            description: "The subtitle file to convert (e.g., .ttml, .srt, .vtt)")
        { IsRequired = true };

        var outputOption = new Option<FileInfo?>(
            name: "--output",
            description: "The output JSON file (optional; defaults to stdout)");

        var formatOption = new Option<string>(
            name: "--format",
            description: "Subtitle format (optional: ttml, srt, vtt). Auto-detect if omitted.");

        var rootCommand = new RootCommand("Convert subtitle files to JSON")
        {
            inputOption,
            outputOption,
            formatOption
        };

        rootCommand.SetHandler(HandleConvertAsync, inputOption, outputOption, formatOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Handles the conversion command for the CLI.
    /// </summary>
    private static async Task HandleConvertAsync(FileInfo? input, FileInfo? output, string? format)
    {
        try
        {
            if (input == null)
            {
                Console.Error.WriteLine("Input file is required.");
                return;
            }
            var service = new SubtitleToJsonService();
            await service.ConvertAsync(input, output, format);
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            Console.Error.WriteLine(ex.Message);
        }
    }
}
