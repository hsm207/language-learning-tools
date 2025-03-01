using Microsoft.Extensions.Configuration;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Service for managing application configuration.
/// </summary>
public class ConfigurationService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the ConfigurationService class.
    /// </summary>
    public ConfigurationService()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets<ConfigurationService>()
            .Build();
    }

    /// <summary>
    /// Gets the Google AI model ID from configuration.
    /// </summary>
    public virtual string? GetGoogleAiModelId() => _configuration["GoogleAI:ModelId"];

    /// <summary>
    /// Gets the Google AI API key from configuration.
    /// </summary>
    public virtual string? GetGoogleAiApiKey() => _configuration["GoogleAI:ApiKey"];

    /// <summary>
    /// Checks if the required Google AI configuration is available.
    /// </summary>
    public virtual bool HasRequiredConfiguration() =>
        !string.IsNullOrEmpty(GetGoogleAiModelId()) &&
        !string.IsNullOrEmpty(GetGoogleAiApiKey());
}