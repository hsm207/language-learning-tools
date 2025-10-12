using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Service for managing application configuration.
/// </summary>
internal class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly string? _googleAiApiKey;
    private readonly string? _googleAiModelId;
    private readonly ParseResult? _parseResult;

    /// <summary>
    /// Initializes a new instance of the ConfigurationService class.
    /// </summary>
    public ConfigurationService() : this(null, null) { }

    /// <summary>
    /// Initializes a new instance of the ConfigurationService class.
    /// </summary>
    public ConfigurationService(string? googleAiApiKey = null, string? googleAiModelId = null)
    {
        _googleAiApiKey = googleAiApiKey;
        _googleAiModelId = googleAiModelId;
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<ConfigurationService>()
            .Build();
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationService class.
    /// </summary>
    public ConfigurationService(ParseResult parseResult)
    {
        _parseResult = parseResult;

        // Find options by name instead of using our own Option instances
        _googleAiApiKey = parseResult.CommandResult.Children
            .OfType<OptionResult>()
            .FirstOrDefault(o => o.Option.Name == "google-ai-api-key")
            ?.GetValueOrDefault<string>();

        _googleAiModelId = parseResult.CommandResult.Children
            .OfType<OptionResult>()
            .FirstOrDefault(o => o.Option.Name == "google-ai-model-id")
            ?.GetValueOrDefault<string>();

        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<ConfigurationService>()
            .Build();
    }

    /// <summary>
    /// Gets the Google AI model ID from configuration.
    /// </summary>
    public virtual string? GetGoogleAiModelId() =>
        _googleAiModelId ?? _configuration["GoogleAI:ModelId"];

    /// <summary>
    /// Gets the Google AI API key from configuration.
    /// </summary>
    public virtual string? GetGoogleAiApiKey() =>
        _googleAiApiKey ?? _configuration["GoogleAI:ApiKey"];

    /// <summary>
    /// Checks if the required Google AI configuration is available.
    /// </summary>
    public virtual bool HasRequiredConfiguration() =>
        !string.IsNullOrEmpty(GetGoogleAiModelId()) &&
        !string.IsNullOrEmpty(GetGoogleAiApiKey());
}