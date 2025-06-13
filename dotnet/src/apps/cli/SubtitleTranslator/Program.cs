using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using LanguageLearningTools.Application;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;

namespace SubtitleTranslator;

/// <summary>
/// Entry point for the SubtitleTranslator CLI tool.
/// Translates subtitle files from one language to another.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point. Sets up dependency injection and CLI commands.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
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

        var requestDelayOption = new Option<int>(
            name: "--request-delay",
            description: "Delay between API requests in milliseconds to avoid rate limiting (default: 7500ms for 8 RPM)",
            getDefaultValue: () => 7500);

        var rootCommand = new RootCommand("Translate subtitle files from one language to another. Outputs JSON with both original and translated text for downstream processing.")
        {
            inputOption,
            outputOption,
            sourceLanguageOption,
            targetLanguageOption,
            subtitleFormatOption,
            apiKeyOption,
            verboseOption,
            requestDelayOption
        };

        rootCommand.SetHandler(
            async (input, output, sourceLanguage, targetLanguage, subtitleFormat, apiKey, verbose, requestDelay) =>
                await HandleTranslateAsync(input, output, sourceLanguage, targetLanguage, subtitleFormat, apiKey, verbose, requestDelay),
            inputOption, 
            outputOption, 
            sourceLanguageOption, 
            targetLanguageOption, 
            subtitleFormatOption,
            apiKeyOption,
            verboseOption,
            requestDelayOption);

        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Configures services for dependency injection with the provided API key and logging level.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="apiKey">The Google Gemini API key.</param>
    /// <param name="verbose">Whether to enable verbose logging.</param>
    /// <param name="requestDelay">The delay between API requests in milliseconds.</param>
    private static void ConfigureServices(IServiceCollection services, string apiKey, bool verbose, int requestDelay)
    {
        // Register logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            if (verbose)
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            }
            else
            {
                builder.SetMinimumLevel(LogLevel.Warning);
            }
        });

        // Register Semantic Kernel with Gemini
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash-preview-05-20", apiKey);
        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel);

        // Register domain services
        services.AddTransient<ISubtitleBatchingStrategy, RollingWindowBatchingStrategy>();
        
        // Register infrastructure services
        services.AddTransient<ISubtitleParser, TtmlSubtitleParser>();
        services.AddTransient<ISubtitleTranslationService>(provider =>
        {
            var kernel = provider.GetRequiredService<Kernel>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var delay = TimeSpan.FromMilliseconds(requestDelay);
            return new GeminiSubtitleTranslationService(kernel, temperature: 0.2, requestDelay: delay, loggerFactory: loggerFactory);
        });
        
        // Register application services
        services.AddTransient<SubtitleTranslationApplicationService>();
    }

    /// <summary>
    /// Handles the translate command for the CLI.
    /// </summary>
    /// <param name="input">Input subtitle file.</param>
    /// <param name="output">Output JSON file for translated subtitles.</param>
    /// <param name="sourceLanguage">Source language code.</param>
    /// <param name="targetLanguage">Target language code.</param>
    /// <param name="subtitleFormat">Input subtitle format (optional).</param>
    /// <param name="apiKey">Google Gemini API key (optional).</param>
    /// <param name="verbose">Enable verbose debug output.</param>
    /// <param name="requestDelay">Delay between API requests in milliseconds.</param>
    private static async Task HandleTranslateAsync(
        FileInfo? input, 
        FileInfo? output, 
        string sourceLanguage, 
        string targetLanguage, 
        string? subtitleFormat,
        string? apiKey,
        bool verbose,
        int requestDelay)
    {
        try
        {
            if (verbose)
            {
                Console.WriteLine("🔍 Debug: Starting subtitle translation process...");
                Console.WriteLine($"🔍 Debug: Input file: {input?.FullName ?? "null"}");
                Console.WriteLine($"🔍 Debug: Output file: {output?.FullName ?? "auto-generated"}");
                Console.WriteLine($"🔍 Debug: Source language: {sourceLanguage}");
                Console.WriteLine($"🔍 Debug: Target language: {targetLanguage}");
                Console.WriteLine($"🔍 Debug: Subtitle format: {subtitleFormat ?? "auto-detect"}");
                Console.WriteLine($"🔍 Debug: API key provided: {(!string.IsNullOrWhiteSpace(apiKey) ? "Yes" : "Via environment")}");
                Console.WriteLine($"🔍 Debug: Request delay: {requestDelay}ms");
            }

            if (input == null)
            {
                Console.Error.WriteLine("Input file is required.");
                return;
            }

            if (!input.Exists)
            {
                Console.Error.WriteLine($"Input file not found: {input.FullName}");
                return;
            }

            if (verbose)
            {
                Console.WriteLine($"🔍 Debug: Input file exists and is readable");
                Console.WriteLine($"🔍 Debug: Input file size: {input.Length} bytes");
            }

            // Parse language codes
            if (!LangExtensions.TryParseFromCode(sourceLanguage, out var sourceLang))
            {
                Console.Error.WriteLine($"Invalid source language code: {sourceLanguage}");
                return;
            }

            if (!LangExtensions.TryParseFromCode(targetLanguage, out var targetLang))
            {
                Console.Error.WriteLine($"Invalid target language code: {targetLanguage}");
                return;
            }

            if (verbose)
            {
                Console.WriteLine($"🔍 Debug: Parsed source language: {sourceLang} ({sourceLang.GetDisplayName()})");
                Console.WriteLine($"🔍 Debug: Parsed target language: {targetLang} ({targetLang.GetDisplayName()})");
            }

            // Check for API key
            var geminiApiKey = apiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(geminiApiKey))
            {
                Console.Error.WriteLine("Google Gemini API key is required. Provide it via --api-key argument or GEMINI_API_KEY environment variable.");
                return;
            }

            if (verbose)
            {
                Console.WriteLine($"🔍 Debug: API key obtained successfully (length: {geminiApiKey.Length} characters)");
            }

            // Generate output filename if not provided
            output ??= GenerateOutputFileName(input);

            if (verbose)
            {
                Console.WriteLine($"🔍 Debug: Final output file: {output.FullName}");
            }

            // Set up dependency injection with the runtime API key
            var services = new ServiceCollection();
            ConfigureServices(services, geminiApiKey, verbose, requestDelay);
            var serviceProvider = services.BuildServiceProvider();

            if (verbose)
            {
                Console.WriteLine("🔍 Debug: Dependency injection container configured successfully");
            }

            // Get the application service and delegate the work
            var translationService = serviceProvider.GetRequiredService<SubtitleTranslationApplicationService>();
            
            Console.WriteLine($"Translating {input.Name} from {sourceLang.GetDisplayName()} to {targetLang.GetDisplayName()}...");
            
            if (verbose)
            {
                Console.WriteLine("🔍 Debug: Starting translation process...");
            }
            
            await translationService.TranslateSubtitleFileAsync(
                input.FullName,
                output.FullName,
                sourceLang,
                targetLang);

            Console.WriteLine($"Translation completed! Output saved to: {output.FullName}");
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine($"File not found: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"🔍 Debug: Full exception details: {ex}");
            }
        }
        catch (NotSupportedException ex)
        {
            Console.Error.WriteLine($"Format not supported: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"🔍 Debug: Full exception details: {ex}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.Error.WriteLine($"Operation failed: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"🔍 Debug: Full exception details: {ex}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"🔍 Debug: Full exception details: {ex}");
            }
        }
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
