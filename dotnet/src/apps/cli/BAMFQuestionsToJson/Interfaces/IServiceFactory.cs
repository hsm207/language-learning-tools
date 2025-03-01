using LanguageLearningTools.BAMFQuestionsToJson.Commands;
using LanguageLearningTools.BAMFQuestionsToJson.Services;
using Microsoft.SemanticKernel;

namespace LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

/// <summary>
/// Interface for a factory that creates service dependencies.
/// </summary>
internal interface IServiceFactory
{
    /// <summary>
    /// Creates a Semantic Kernel instance.
    /// </summary>
    /// <returns>A configured Kernel instance or null if configuration is unavailable.</returns>
    Kernel? CreateKernel();

    /// <summary>
    /// Creates an ImageProcessor instance.
    /// </summary>
    /// <returns>A configured IImageProcessor instance.</returns>
    IImageProcessor CreateImageProcessor();

    /// <summary>
    /// Creates a CommandInvoker instance.
    /// </summary>
    /// <returns>A new CommandInvoker instance.</returns>
    CommandInvoker CreateCommandInvoker();

    /// <summary>
    /// Creates a ProcessFileBatchCommand instance.
    /// </summary>
    /// <param name="imageProcessor">The image processor to use.</param>
    /// <param name="inputDirectory">Directory containing question screenshots.</param>
    /// <param name="outputFilePath">Path where the JSON output file will be saved.</param>
    /// <param name="limit">Maximum number of files to process (optional).</param>
    /// <param name="batchSize">Number of files to process before saving interim results.</param>
    /// <returns>A configured ProcessFileBatchCommand instance.</returns>
    ProcessFileBatchCommand CreateProcessFileBatchCommand(
        IImageProcessor imageProcessor,
        string inputDirectory,
        string outputFilePath,
        int? limit,
        int batchSize);

    /// <summary>
    /// Checks if a Google AI key is configured.
    /// </summary>
    /// <returns>True if a Google AI key is configured, otherwise false.</returns>
    bool HasGoogleAiKey();

    /// <summary>
    /// Checks if a Google AI model is configured.
    /// </summary>
    /// <returns>True if a Google AI model is configured, otherwise false.</returns>
    bool HasGoogleAiModel();
}