using System.CommandLine;
using BAMFJsonToMarkdown.Commands;

namespace BAMFJsonToMarkdown;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await CreateCommandLine().InvokeAsync(args);
    }

    private static RootCommand CreateCommandLine()
    {
        var inputOption = new Option<FileInfo>(
            name: "--input",
            description: "The JSON file to convert to Markdown.");

        var outputOption = new Option<FileInfo>(
            name: "--output",
            description: "The output Markdown file.");

        var rootCommand = new RootCommand("Converts BAMF JSON to Markdown table")
        {
            inputOption,
            outputOption
        };

        rootCommand.SetHandler(async (input, output) =>
        {
            var command = new JsonToMarkdownCommand(input, output);
            await command.ExecuteAsync();
        }, inputOption, outputOption);

        return rootCommand;
    }
}
