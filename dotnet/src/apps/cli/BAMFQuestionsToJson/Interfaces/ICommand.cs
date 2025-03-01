namespace LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

/// <summary>
/// Defines the contract for all commands in the application.
/// </summary>
internal interface ICommand
{
    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync();

    /// <summary>
    /// Gets the unique identifier for this command instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets a description of what this command does.
    /// </summary>
    string Description { get; }
}