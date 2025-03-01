using System.CommandLine;
using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

namespace LanguageLearningTools.BAMFQuestionsToJson.Configuration;

/// <summary>
/// Configures command line options and commands.
/// </summary>
internal class CommandLineConfiguration : ICommandLineConfiguration
{
    /// <summary>
    /// Creates and configures a root command with all necessary options.
    /// </summary>
    /// <returns>A configured RootCommand instance.</returns>
    public RootCommand BuildRootCommand()
    {
        var inputOption = new Option<string>(
            name: "--input",
            description: "Directory containing question screenshots",
            getDefaultValue: () => "screenshots"
        );

        var outputOption = new Option<string>(
            name: "--output",
            description: "Output JSON file path",
            getDefaultValue: () => "bamf_questions.json"
        );

        var limitOption = new Option<int?>(
            name: "--limit",
            description: "Maximum number of files to process (optional)",
            getDefaultValue: () => null
        );

        var batchSizeOption = new Option<int>(
            name: "--batch-size",
            description: "Number of files to process before saving interim results",
            getDefaultValue: () => 100
        );

        var googleAiApiKeyOption = new Option<string?>(
            name: "--google-ai-api-key",
            description: "Google AI API key (overrides value in secret manager)",
            getDefaultValue: () => null
        );

        var googleAiModelIdOption = new Option<string?>(
            name: "--google-ai-model-id",
            description: "Google AI model ID (overrides value in secret manager)",
            getDefaultValue: () => null
        );

        var rootCommand = new RootCommand("Convert BAMF question screenshots to JSON");
        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(limitOption);
        rootCommand.AddOption(batchSizeOption);
        rootCommand.AddOption(googleAiApiKeyOption);
        rootCommand.AddOption(googleAiModelIdOption);

        return rootCommand;
    }

    /// <summary>
    /// Registers the command handler with the provided service factory.
    /// </summary>
    /// <param name="rootCommand">The command to register the handler with.</param>
    /// <param name="serviceFactory">The service factory to use for dependency resolution.</param>
    public void RegisterCommandHandler(RootCommand rootCommand, IServiceFactory serviceFactory)
    {
        rootCommand.SetHandler(
            async (string input, string output, int? limit, int batchSize, string? googleAiApiKey, string? googleAiModelId) =>
            {
                if (string.IsNullOrEmpty(googleAiApiKey) && !serviceFactory.HasGoogleAiKey())
                    throw new ArgumentException("Google AI API key must be provided either via --google-ai-api-key or secret manager");

                if (string.IsNullOrEmpty(googleAiModelId) && !serviceFactory.HasGoogleAiModel())
                    throw new ArgumentException("Google AI model ID must be provided either via --google-ai-model-id or secret manager");

                await ProgramOrchestrator.ExecuteProcessingCommandAsync(
                    serviceFactory,
                    input,
                    output,
                    limit,
                    batchSize).ConfigureAwait(false);
            },
            rootCommand.Options.OfType<Option<string>>().First(o => o.Name == "input"),
            rootCommand.Options.OfType<Option<string>>().First(o => o.Name == "output"),
            rootCommand.Options.OfType<Option<int?>>().First(o => o.Name == "limit"),
            rootCommand.Options.OfType<Option<int>>().First(o => o.Name == "batch-size"),
            rootCommand.Options.OfType<Option<string?>>().First(o => o.Name == "google-ai-api-key"),
            rootCommand.Options.OfType<Option<string?>>().First(o => o.Name == "google-ai-model-id")
        );
    }
}