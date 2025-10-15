using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using LanguageLearningTools.Infrastructure; // For TtmlSubtitleParser and SubtitleJsonSerializer
using LanguageLearningTools.Domain;

namespace SubtitleTool.Commands
{
    public class ConvertCommand : Command
    {
        public ConvertCommand() : base("convert", "Convert subtitle files to JSON")
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

            AddOption(inputOption);
            AddOption(outputOption);
            AddOption(formatOption);

            this.SetHandler(async (context) =>
            {
                var input = context.ParseResult.GetValueForOption(inputOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var format = context.ParseResult.GetValueForOption(formatOption);
                context.ExitCode = await HandleConvertAsync(input, output, format);
            });
        }

        internal static async Task<int> HandleConvertAsync(FileInfo? input, FileInfo? output, string? format, ISubtitleParser? parser = null)
        {
            try
            {
                if (input == null)
                {
                    Console.Error.WriteLine("Input file is required.");
                    return 1;
                }

                // SubtitleToJsonService logic
                if (input == null || !input.Exists)
                {
                    Console.Error.WriteLine($"Input file not found: {input?.FullName}");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(format) || format.ToLowerInvariant() == "ttml")
                {
                    parser ??= new TtmlSubtitleParser();
                    using var inputStream = input.OpenRead();
                    var lines = await parser.ParseAsync(inputStream);
                    var json = SubtitleJsonSerializer.ToJson(lines);

                    if (output != null)
                    {
                        await File.WriteAllTextAsync(output.FullName, json);
                    }
                    else
                    {
                        Console.WriteLine(json);
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Format '{format}' is not supported.");
                    return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            return 0;
        }
    }
}
