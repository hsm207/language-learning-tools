using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// Base abstract class for all commands in the application.
/// </summary>
internal abstract class CommandBase : ICommand
{
    /// <summary>
    /// Gets the unique identifier for this command instance.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets a description of what this command does.
    /// </summary>
    public virtual string Description { get; }

    /// <summary>
    /// Initializes a new instance of the CommandBase class.
    /// </summary>
    /// <param name="description">A description of what this command does.</param>
    protected CommandBase(string description)
    {
        Id = Guid.NewGuid().ToString();
        Description = description;
    }

    /// <summary>
    /// Executes the command asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task ExecuteAsync();
}