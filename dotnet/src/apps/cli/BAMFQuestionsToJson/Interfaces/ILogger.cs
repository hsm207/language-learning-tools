namespace LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

/// <summary>
/// Interface for logging messages at different severity levels.
/// </summary>
internal interface ILogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogInformation(string message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogWarning(string message);

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    void LogError(string message);
}