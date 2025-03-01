using LanguageLearningTools.BAMFQuestionsToJson.Models;
using Spectre.Console;
using System.Text.Json;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// Command for saving questions to a JSON file.
/// </summary>
internal sealed class SaveQuestionsCommand : CommandBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly List<BamfQuestion> _questions;
    private readonly string _outputPath;

    /// <summary>
    /// Initializes a new instance of the SaveQuestionsCommand class.
    /// </summary>
    /// <param name="questions">The questions to save.</param>
    /// <param name="outputPath">The path to save the JSON file to.</param>
    public SaveQuestionsCommand(List<BamfQuestion> questions, string outputPath)
        : base($"Save {questions.Count} questions to {outputPath}")
    {
        _questions = questions ?? throw new ArgumentNullException(nameof(questions));
        _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
    }

    /// <summary>
    /// Executes the command to save questions to a JSON file.
    /// </summary>
    public override async Task ExecuteAsync()
    {
        try
        {
            // Create backup of existing file if it exists
            if (File.Exists(_outputPath))
            {
                string backupPath = $"{_outputPath}.bak";
                File.Copy(_outputPath, backupPath, true);
                AnsiConsole.MarkupLine($"Created backup at [blue]{backupPath}[/]");
            }

            // Write to a temporary file first to prevent corruption if the process is interrupted
            string tempPath = $"{_outputPath}.temp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(_questions, JsonOptions)).ConfigureAwait(false);

            // Replace the original file with the temporary file
            if (File.Exists(_outputPath))
            {
                File.Delete(_outputPath);
            }
            File.Move(tempPath, _outputPath);
            AnsiConsole.MarkupLine($"Saved [green]{_questions.Count}[/] questions to [blue]{_outputPath}[/]");
        }
        catch (UnauthorizedAccessException ex)
        {
            AnsiConsole.MarkupLine($"[red]Access denied while saving questions: {ex.Message}[/]");
            throw;
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]IO error while saving questions: {ex.Message}[/]");
            throw;
        }
    }
}