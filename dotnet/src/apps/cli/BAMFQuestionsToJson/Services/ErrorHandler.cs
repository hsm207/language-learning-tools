using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Implementation of IErrorHandler that handles exceptions and logs errors.
/// </summary>
public class ErrorHandler : IErrorHandler
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the ErrorHandler class.
    /// </summary>
    /// <param name="logger">The logger to use for error messages.</param>
    public ErrorHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles an exception that occurred during program execution.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>An exit code representing the error.</returns>
    public int HandleException(Exception exception)
    {
        LogError(exception.Message);

        // Map different exception types to different exit codes
        return exception switch
        {
            ArgumentException => 1,
            InvalidOperationException => 2,
            FileNotFoundException => 3,
            DirectoryNotFoundException => 4,
            UnauthorizedAccessException => 5,
            // Default for any other exception
            _ => 99
        };
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    public void LogError(string message)
    {
        _logger.LogError($"Error: {message}");
    }
}