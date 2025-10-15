using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LanguageLearningTools.Application;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using SubtitleTool.Configuration;

namespace SubtitleTool.Commands
{
    public class TranslateCommand : Command
    {
        public TranslateCommand()
            : base("translate", "Translate subtitle files from one language to another. Outputs JSON with both original and translated text for downstream processing.")
        {
            var inputOption = new Option<FileInfo?>(
                name: "--input",
                description: "The subtitle file to translate (e.g., .ttml, .srt, .vtt)")
            { IsRequired = true };

            var outputOption = new Option<FileInfo?>(
                name: "--output",
                description: "The output JSON file for translated subtitles (optional; defaults to input filename with '_translated.json' suffix)");

            var sourceLanguageOption = new Option<string>(
                name: "--source-language",
                description: "Source language code for translation (e.g., 'de' for German, 'german')")
            { IsRequired = true };

            var targetLanguageOption = new Option<string>(
                name: "--target-language",
                description: "Target language code for translation (e.g., 'en' for English, 'english')")
            { IsRequired = true };

            var subtitleFormatOption = new Option<string>(
                name: "--subtitle-format",
                description: "Input subtitle format (optional: ttml, srt, vtt). Auto-detected from file extension if omitted.");

            var apiKeyOption = new Option<string>(
                name: "--api-key",
                description: "Google Gemini API key for translation (optional; uses GEMINI_API_KEY environment variable if not provided)");

            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Enable verbose output for debugging");

            var requestsPerMinuteOption = new Option<int>(
                name: "--requests-per-minute",
                description: "Maximum requests per minute to avoid rate limiting (default: 8 RPM for Gemini API)",
                getDefaultValue: () => 8);

            AddOption(inputOption);
            AddOption(outputOption);
            AddOption(sourceLanguageOption);
            AddOption(targetLanguageOption);
            AddOption(subtitleFormatOption);
            AddOption(apiKeyOption);
            AddOption(verboseOption);
            AddOption(requestsPerMinuteOption);

            this.SetHandler(async (context) =>
            {
                var input = context.ParseResult.GetValueForOption(inputOption);
                var output = context.ParseResult.GetValueForOption(outputOption);
                var sourceLanguage = context.ParseResult.GetValueForOption(sourceLanguageOption);
                var targetLanguage = context.ParseResult.GetValueForOption(targetLanguageOption);
                var subtitleFormat = context.ParseResult.GetValueForOption(subtitleFormatOption);
                var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);
                var requestsPerMinute = context.ParseResult.GetValueForOption(requestsPerMinuteOption);

                var services = new ServiceCollection();
                services.AddTranslationServices(apiKey, verbose, requestsPerMinute);
                await using var serviceProvider = services.BuildServiceProvider();

                context.ExitCode = await HandleTranslateAsync(input, output, sourceLanguage!, targetLanguage!, subtitleFormat, verbose, requestsPerMinute, serviceProvider);
            });
        }

        internal static async Task<int> HandleTranslateAsync(
            FileInfo? input,
            FileInfo? output,
            string sourceLanguage,
            string targetLanguage,
            string? subtitleFormat,
            bool verbose,
            int requestsPerMinute,
            IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<TranslateCommand>>();

            try
            {
                logger.LogDebug("Starting subtitle translation process");
                logger.LogDebug("Input file: {InputFile}", input?.FullName ?? "null");
                logger.LogDebug("Output file: {OutputFile}", output?.FullName ?? "auto-generated");
                logger.LogDebug("Source language: {SourceLanguage}", sourceLanguage);
                logger.LogDebug("Target language: {TargetLanguage}", targetLanguage);
                logger.LogDebug("Subtitle format: {SubtitleFormat}", subtitleFormat ?? "auto-detect");
                logger.LogDebug("Requests per minute: {RequestsPerMinute} RPM", requestsPerMinute);

                if (input == null)
                {
                    Console.Error.WriteLine("Input file is required.");
                    return 1;
                }

                if (!input.Exists)
                {
                    logger.LogError("Input file not found: {InputFile}", input.FullName);
                    Console.Error.WriteLine($"Input file not found: {input.FullName}");
                    return 1;
                }

                logger.LogDebug("Input file exists and is readable");
                logger.LogDebug("Input file size: {FileSize} bytes", input.Length);

                // Parse language codes
                if (!LangExtensions.TryParseFromCode(sourceLanguage, out var sourceLang))
                {
                    Console.Error.WriteLine($"Invalid source language code: {sourceLanguage}");
                    return 1;
                }

                if (!LangExtensions.TryParseFromCode(targetLanguage, out var targetLang))
                {
                    Console.Error.WriteLine($"Invalid target language code: {targetLanguage}");
                    return 1;
                }

                logger.LogDebug("Parsed source language: {SourceLang} ({SourceLangDisplay})", sourceLang, sourceLang.GetDisplayName());
                logger.LogDebug("Parsed target language: {TargetLang} ({TargetLangDisplay})", targetLang, targetLang.GetDisplayName());



                // Generate output filename if not provided
                output ??= GenerateOutputFileName(input);
                logger.LogDebug("Final output file: {OutputFile}", output.FullName);

                logger.LogDebug("Dependency injection container configured successfully");

                // Get the application service and delegate the work
                var translationService = serviceProvider.GetRequiredService<SubtitleTranslationApplicationService>();

                Console.WriteLine($"Translating {input.Name} from {sourceLang.GetDisplayName()} to {targetLang.GetDisplayName()}...");
                logger.LogInformation("Starting translation of {InputFile} from {SourceLanguage} to {TargetLanguage}",
                    input.Name, sourceLang.GetDisplayName(), targetLang.GetDisplayName());

                try
                {
                    await translationService.TranslateSubtitleFileAsync(
                        input.FullName,
                        output.FullName,
                        sourceLang,
                        targetLang);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred during translation.");
                    Console.Error.WriteLine(ex.Message);
                    return 1;
                }

                Console.WriteLine($"Translation completed! Output saved to: {output.FullName}");
                logger.LogInformation("Translation completed successfully. Output saved to: {OutputFile}", output.FullName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Generates an output filename based on the input filename.
        /// </summary>
        /// <param name="input">The input file.</param>
        /// <returns>A FileInfo object for the generated output filename with .json extension.</returns>
        public static FileInfo GenerateOutputFileName(FileInfo input)
        {
            var directory = input.DirectoryName ?? "";
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(input.Name);

            var outputFileName = $"{nameWithoutExtension}_translated.json";
            return new FileInfo(Path.Combine(directory, outputFileName));
        }
    }
}
