namespace LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

/// <summary>
/// Interface for handling and reporting errors.
/// </summary>
internal interface IErrorHandler
{
    /// <summary>
    /// Handles an exception that occurred during program execution.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>An exit code representing the error.</returns>
    int HandleException(Exception exception);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The error message to log.</param>
    void LogError(string message);
}