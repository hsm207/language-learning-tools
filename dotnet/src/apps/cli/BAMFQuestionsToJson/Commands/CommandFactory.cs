using LanguageLearningTools.BAMFQuestionsToJson.Commands;
using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// Factory for creating command objects based on specific operations.
/// </summary>
internal static class CommandFactory
{
    /// <summary>
    /// Creates a composite command that runs multiple commands in sequence.
    /// </summary>
    /// <param name="commands">The commands to execute in sequence.</param>
    /// <param name="description">A description of the composite operation.</param>
    /// <returns>A composite command.</returns>
    public static CompositeCommand CreateCompositeCommand(IEnumerable<ICommand> commands, string description)
    {
        return new CompositeCommand(commands, description);
    }
}