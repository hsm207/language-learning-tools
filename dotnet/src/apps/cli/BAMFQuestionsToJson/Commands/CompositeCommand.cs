using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using Spectre.Console;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// A command that executes multiple other commands in sequence.
/// </summary>
internal class CompositeCommand : CommandBase
{
    private readonly List<ICommand> _commands;

    /// <summary>
    /// Initializes a new instance of the CompositeCommand class.
    /// </summary>
    /// <param name="commands">The commands to execute in sequence.</param>
    /// <param name="description">Description of what this composite command does.</param>
    public CompositeCommand(IEnumerable<ICommand> commands, string description)
        : base(description)
    {
        _commands = commands?.ToList() ?? new List<ICommand>();

        if (_commands.Count == 0)
        {
            throw new ArgumentException("At least one command must be provided", nameof(commands));
        }
    }

    /// <summary>
    /// Executes all contained commands in sequence.
    /// </summary>
    public override async Task ExecuteAsync()
    {
        foreach (var command in _commands)
        {
            try
            {
                await command.ExecuteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error executing sub-command {command.Description}: {ex.Message}[/]");
                throw;
            }
        }
    }
}