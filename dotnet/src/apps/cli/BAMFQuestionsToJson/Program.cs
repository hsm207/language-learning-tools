using System.CommandLine;
using System.CommandLine.Parsing;
using LanguageLearningTools.BAMFQuestionsToJson.Commands;
using LanguageLearningTools.BAMFQuestionsToJson.Services;
using LanguageLearningTools.BAMFQuestionsToJson.Factories;
using LanguageLearningTools.BAMFQuestionsToJson.Configuration;
using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

namespace LanguageLearningTools.BAMFQuestionsToJson;

/// <summary>
/// Main program class that handles command line interface for converting BAMF question screenshots to JSON format.
/// Uses the Command pattern for processing operations with dependency injection for testability.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Entry point of the application. Initializes and executes the command line interface.
    /// </summary>
    /// <param name="args">Command line arguments passed to the program.</param>
    /// <returns>0 if execution was successful, non-zero if an error occurred.</returns>
    public static async Task<int> Main(string[] args)
    {
        // Initialize basic services
        ILogger logger = new ConsoleLogger();
        IErrorHandler errorHandler = new ErrorHandler(logger);
        #pragma warning disable CA1859 // Using interface for decoupling.
        ICommandLineConfiguration commandLineConfig = new Configuration.CommandLineConfiguration();
#pragma warning restore CA1859
        
        return await RunApplicationAsync(args, new ServiceFactory(new Parser(commandLineConfig.BuildRootCommand()).Parse(args)), commandLineConfig, logger, errorHandler).ConfigureAwait(false);
    }

    internal static async Task<int> RunApplicationAsync(
        string[] args,
        IServiceFactory serviceFactory,
        ICommandLineConfiguration commandLineConfig,
        ILogger logger,
        IErrorHandler errorHandler)
    {
        try
        {
            // Build and configure command line interface
            var rootCommand = commandLineConfig.BuildRootCommand();
            commandLineConfig.RegisterCommandHandler(rootCommand, serviceFactory);

            // Execute command
            return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return errorHandler.HandleException(ex);
        }
        catch (InvalidOperationException ex)
        {
            return errorHandler.HandleException(ex);
        }
        catch (IOException ex)
        {
            await logger.LogErrorAsync($"File operation error: {ex.Message}").ConfigureAwait(false);
            return 1;
        }
        catch (OutOfMemoryException ex)
        {
            await logger.LogErrorAsync($"Critical error - out of memory: {ex.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (ApplicationException ex)
        {
            await logger.LogErrorAsync($"Application error: {ex.Message}").ConfigureAwait(false);
            return 3;
        }
        #pragma warning disable CA1031 // General exception catch is acceptable for top-level error handling.
        catch (Exception ex)
        {
            return errorHandler.HandleException(ex);
        }
#pragma warning restore CA1031
    }
}
