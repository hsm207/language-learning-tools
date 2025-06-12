using System;
using System.IO;
using System.Threading.Tasks;

namespace SubtitleToJson.Services
{
    /// <summary>
    /// Service for converting subtitle files to JSON.
    /// </summary>
    public class SubtitleToJsonService
    {
        /// <summary>
        /// Converts a subtitle file to JSON and writes to the specified output or stdout.
        /// </summary>
        /// <param name="input">Input subtitle file.</param>
        /// <param name="output">Output JSON file (optional).</param>
        /// <param name="format">Subtitle format (optional, defaults to ttml).</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the input file does not exist.</exception>
        public async Task ConvertAsync(FileInfo input, FileInfo? output, string? format)
        {
            if (input == null || !input.Exists)
                throw new FileNotFoundException("Input file does not exist.", input?.FullName);

            if (string.IsNullOrWhiteSpace(format) || format.ToLowerInvariant() == "ttml")
            {
                var parser = new LanguageLearningTools.Infrastructure.TtmlSubtitleParser();
                using var inputStream = input.OpenRead();
                var lines = await parser.ParseAsync(inputStream);
                var json = LanguageLearningTools.Infrastructure.SubtitleJsonSerializer.ToJson(lines);

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
                throw new NotSupportedException($"Format '{format}' is not supported.");
            }
        }
    }
}
