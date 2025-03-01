using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using Spectre.Console;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Implementation of ILogger that logs messages to the console using Spectre.Console.
/// </summary>
public class ConsoleLogger : ILogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogInformation(string message)
    {
        AnsiConsole.MarkupLine($"[blue]{message}[/]");
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{message}[/]");
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message}[/]");
    }
}