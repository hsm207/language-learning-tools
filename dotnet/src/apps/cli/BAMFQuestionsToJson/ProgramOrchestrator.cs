using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using LanguageLearningTools.BAMFQuestionsToJson.Services;

namespace LanguageLearningTools.BAMFQuestionsToJson;

/// <summary>
/// Orchestrates the execution of the program by coordinating dependencies and workflow.
/// </summary>
internal static class ProgramOrchestrator
{
    /// <summary>
    /// Executes the file processing command with the specified parameters.
    /// </summary>
    /// <param name="serviceFactory">The factory to create services.</param>
    /// <param name="inputDirectory">Directory containing question screenshots.</param>
    /// <param name="outputFile">Path where the JSON output file will be saved.</param>
    /// <param name="limit">Maximum number of files to process (optional).</param>
    /// <param name="batchSize">Number of files to process before saving interim results.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ExecuteProcessingCommandAsync(
        IServiceFactory serviceFactory,
        string inputDirectory,
        string outputFile,
        int? limit,
        int batchSize)
    {
        ArgumentNullException.ThrowIfNull(serviceFactory);
        ArgumentException.ThrowIfNullOrEmpty(inputDirectory);
        ArgumentException.ThrowIfNullOrEmpty(outputFile);

        // Create core dependencies
        var commandInvoker = serviceFactory.CreateCommandInvoker();
        var imageProcessor = serviceFactory.CreateImageProcessor();

        // Create the batch processing command
        var batchCommand = serviceFactory.CreateProcessFileBatchCommand(
            imageProcessor,
            inputDirectory,
            outputFile,
            limit,
            batchSize);

        // Execute the command
        await commandInvoker.InvokeAsync(batchCommand).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the file processing command with the specified parameters, using a logger and error handler.
    /// </summary>
    /// <param name="serviceFactory">The factory to create services.</param>
    /// <param name="logger">The logger to use.</param>
    /// <param name="errorHandler">The error handler to use.</param>
    /// <param name="inputDirectory">Directory containing question screenshots.</param>
    /// <param name="outputFile">Path where the JSON output file will be saved.</param>
    /// <param name="limit">Maximum number of files to process (optional).</param>
    /// <param name="batchSize">Number of files to process before saving interim results.</param>
    /// <returns>An exit code for the application (0 for success, non-zero for errors).</returns>
    public static async Task<int> ExecuteProcessingCommandAsync(
        IServiceFactory serviceFactory,
        ILogger logger,
        IErrorHandler errorHandler,
        string inputDirectory,
        string outputFile,
        int? limit,
        int batchSize)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(serviceFactory);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(errorHandler);
            ArgumentException.ThrowIfNullOrEmpty(inputDirectory);
            ArgumentException.ThrowIfNullOrEmpty(outputFile);

            logger.LogInformation($"Processing screenshots from: {inputDirectory}");
            logger.LogInformation($"Output will be saved to: {outputFile}");

            await ExecuteProcessingCommandAsync(
                serviceFactory,
                inputDirectory,
                outputFile,
                limit,
                batchSize).ConfigureAwait(false);

            return 0;
        }
        catch (ArgumentException ex)
        {
            logger.LogError($"Invalid argument: {ex.Message}");
            return errorHandler.HandleException(ex);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError($"Operation error: {ex.Message}");
            return errorHandler.HandleException(ex);
        }
        catch (IOException ex)
        {
            logger.LogError($"IO error: {ex.Message}");
            return errorHandler.HandleException(ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError($"Access denied: {ex.Message}");
            return errorHandler.HandleException(ex);
        }
    }
}