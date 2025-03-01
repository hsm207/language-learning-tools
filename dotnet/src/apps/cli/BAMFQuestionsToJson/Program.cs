using System.CommandLine;
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
public class Program
{
    /// <summary>
    /// Entry point of the application. Initializes and executes the command line interface.
    /// </summary>
    /// <param name="args">Command line arguments passed to the program.</param>
    /// <returns>0 if execution was successful, non-zero if an error occurred.</returns>
    public static async Task<int> Main(string[] args)
    {
        try
        {
            // Create services
            IServiceFactory serviceFactory = new ServiceFactory();
            ICommandLineConfiguration commandLineConfig = new Configuration.CommandLineConfiguration();
            ILogger logger = new ConsoleLogger();
            IErrorHandler errorHandler = new ErrorHandler(logger);

            return await RunApplicationAsync(args, serviceFactory, commandLineConfig, logger, errorHandler);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled error: {ex.Message}");
            return 99;
        }
    }

    /// <summary>
    /// Runs the application with the provided dependencies.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <param name="serviceFactory">The service factory to use.</param>
    /// <param name="commandLineConfig">The command line configuration to use.</param>
    /// <param name="logger">The logger to use.</param>
    /// <param name="errorHandler">The error handler to use.</param>
    /// <returns>Exit code for the application.</returns>
    public static async Task<int> RunApplicationAsync(
        string[] args,
        IServiceFactory serviceFactory,
        ICommandLineConfiguration commandLineConfig,
        ILogger logger,
        IErrorHandler errorHandler)
    {
        try
        {
            // Build command line interface
            var rootCommand = commandLineConfig.BuildRootCommand();

            // Register command handler with service factory
            commandLineConfig.RegisterCommandHandler(rootCommand, serviceFactory);

            // Execute command
            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            return errorHandler.HandleException(ex);
        }
    }
}
