using Microsoft.SemanticKernel;
using LanguageLearningTools.BAMFQuestionsToJson.Services;

namespace LanguageLearningTools.BAMFQuestionsToJson.Factories;

/// <summary>
/// Factory class for creating configured instances of the Semantic Kernel.
/// Handles configuration loading and initialization of the Google AI Gemini model.
/// </summary>
internal static class KernelFactory
{
    private const string ConfigurationMissingError = "Error: GoogleAI configuration is missing.";

    /// <summary>
    /// Creates a new instance of the Semantic Kernel configured with Google AI Gemini settings.
    /// Uses ConfigurationService to retrieve model ID and API key.
    /// </summary>
    /// <returns>
    /// A configured Kernel instance if the required configuration is present;
    /// otherwise, null and prints an error message to the console.
    /// </returns>
    public static Kernel? Create()
    {
        var configService = new ConfigurationService();
        return Create(configService);
    }

    /// <summary>
    /// Creates a new instance of the Semantic Kernel configured with Google AI Gemini settings
    /// using the provided configuration service.
    /// </summary>
    /// <param name="configService">The configuration service to use for retrieving settings.</param>
    /// <returns>
    /// A configured Kernel instance if the required configuration is present;
    /// otherwise, null and prints an error message to the console.
    /// </returns>
    public static Kernel? Create(ConfigurationService configService)
    {
        try
        {
            if (!configService.HasRequiredConfiguration())
            {
                Console.WriteLine(ConfigurationMissingError);
                return null;
            }

#pragma warning disable SKEXP0070
            var kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(
                    modelId: configService.GetGoogleAiModelId()!,
                    apiKey: configService.GetGoogleAiApiKey()!)
                .Build();
#pragma warning restore SKEXP0070

            return kernel;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid configuration: {ex.Message}");
            return null;
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error initializing kernel: {ex.Message}");
            return null;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Network error creating kernel: {ex.Message}");
            return null;
        }
    }
}