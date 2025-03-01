using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using Spectre.Console;
using System.Collections.Concurrent;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// Responsible for invoking commands and tracking their execution.
/// </summary>
internal sealed class CommandInvoker
{
    private readonly ConcurrentDictionary<string, ICommand> _activeCommands = new();

    /// <summary>
    /// Executes a command asynchronously and tracks its execution.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        AnsiConsole.MarkupLine($"[blue]Starting command:[/] {command.Description}");
        _activeCommands.TryAdd(command.Id, command);

        try
        {
            await command.ExecuteAsync().ConfigureAwait(false);
            AnsiConsole.MarkupLine($"[green]Command completed:[/] {command.Description}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Command failed:[/] {command.Description}. Error: {ex.Message}");
            throw;
        }
        finally
        {
            _activeCommands.TryRemove(command.Id, out _);
        }
    }

    /// <summary>
    /// Gets all currently active commands.
    /// </summary>
    /// <returns>A collection of active commands.</returns>
    public IEnumerable<ICommand> GetActiveCommands()
    {
        return _activeCommands.Values;
    }
}