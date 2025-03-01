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
        try
        {
            // Initialize basic services
            ILogger logger = new ConsoleLogger();
            ErrorHandler errorHandler = new ErrorHandler(logger);
            var commandLineConfig = new Configuration.CommandLineConfiguration();

            // Build and configure command line interface
            var rootCommand = commandLineConfig.BuildRootCommand();

            try
            {
                // Create service factory with parsed configuration
                ServiceFactory serviceFactory = new ServiceFactory(rootCommand.Parse(args));
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
                await Console.Error.WriteLineAsync($"File operation error: {ex.Message}").ConfigureAwait(false);
                return 1;
            }
        }
        catch (OutOfMemoryException ex)
        {
            await Console.Error.WriteLineAsync($"Critical error - out of memory: {ex.Message}").ConfigureAwait(false);
            return 2;
        }
        catch (ApplicationException ex)
        {
            await Console.Error.WriteLineAsync($"Application error: {ex.Message}").ConfigureAwait(false);
            return 3;
        }
    }
}
