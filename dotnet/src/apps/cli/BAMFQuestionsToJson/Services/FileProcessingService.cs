using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using LanguageLearningTools.BAMFQuestionsToJson.Models;
using Spectre.Console;
using System.Text.Json;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Service for processing files and generating the output JSON.
/// </summary>
internal sealed class FileProcessingService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IImageProcessor _imageProcessor;
    private const int DEFAULT_BATCH_SIZE = 100;

    /// <summary>
    /// Initializes a new instance of the FileProcessingService class.
    /// </summary>
    /// <param name="imageProcessor">The image processor to use.</param>
    public FileProcessingService(IImageProcessor imageProcessor)
    {
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
    }

    /// <summary>
    /// Processes all PNG files in the specified input directory.
    /// </summary>
    /// <param name="inputDirectory">Directory path containing question screenshots.</param>
    /// <param name="outputFilePath">Path where the JSON output file will be saved.</param>
    /// <param name="limit">Maximum number of files to process (optional).</param>
    /// <param name="batchSize">Number of files to process before saving interim results (default: 100).</param>
    /// <param name="resumeFromIndex">Index to resume processing from (for recovery after failure).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProcessFilesAsync(
        string inputDirectory,
        string outputFilePath,
        int? limit,
        int batchSize = DEFAULT_BATCH_SIZE,
        int resumeFromIndex = 0)
    {
        try
        {
            AnsiConsole.MarkupLine($"Processing screenshots from: [green]{inputDirectory}[/]");
            AnsiConsole.MarkupLine($"Output will be saved to: [green]{outputFilePath}[/]");
            if (limit.HasValue)
            {
                AnsiConsole.MarkupLine($"Processing up to [yellow]{limit}[/] files");
            }
            AnsiConsole.MarkupLine($"Using batch size of [blue]{batchSize}[/] files");
            if (resumeFromIndex > 0)
            {
                AnsiConsole.MarkupLine($"Resuming from file index [yellow]{resumeFromIndex}[/]");
            }

            // Get all PNG files sorted by creation time
            string[] allImageFiles = GetImageFiles(inputDirectory, limit);

            if (allImageFiles.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]No image files found to process.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"Found [green]{allImageFiles.Length}[/] files to process");

            var questions = await LoadExistingQuestionsAsync(outputFilePath, resumeFromIndex).ConfigureAwait(false);
            await ProcessImageFilesAsync(allImageFiles, questions, outputFilePath, batchSize, resumeFromIndex).ConfigureAwait(false);
        }
        catch (DirectoryNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Directory not found: {ex.Message}[/]");
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            AnsiConsole.MarkupLine($"[red]Access denied: {ex.Message}[/]");
            throw;
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]IO error: {ex.Message}[/]");
            throw;
        }
    }

    public static async Task<List<BamfQuestion>> LoadExistingQuestionsAsync(string outputFilePath, int resumeFromIndex)
    {
        var questions = new List<BamfQuestion>();
        if (resumeFromIndex > 0 && File.Exists(outputFilePath))
        {
            try
            {
                string existingJson = await File.ReadAllTextAsync(outputFilePath).ConfigureAwait(false);
                questions = JsonSerializer.Deserialize<List<BamfQuestion>>(existingJson, JsonOptions) ?? new List<BamfQuestion>();
                AnsiConsole.MarkupLine($"Loaded [green]{questions.Count}[/] existing questions from output file");
            }
            catch (JsonException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error parsing existing questions: {ex.Message}[/]");
                AnsiConsole.MarkupLine("Starting with empty question list");
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error reading existing questions file: {ex.Message}[/]");
                AnsiConsole.MarkupLine("Starting with empty question list");
            }
        }
        return questions;
    }

    private async Task ProcessImageFilesAsync(string[] allImageFiles, List<BamfQuestion> questions, string outputFilePath, int batchSize, int resumeFromIndex)
    {
        int totalProcessed = Math.Min(resumeFromIndex, questions.Count);
        int successCount = totalProcessed;
        int failCount = 0;
        var filesRemaining = allImageFiles.Length - resumeFromIndex;

        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new SpinnerColumn(),
            })
            .StartAsync(async ctx =>
            {
                var progressTask = ctx.AddTask($"[green]Processing Files[/]", maxValue: filesRemaining);
                if (resumeFromIndex > 0)
                {
                    progressTask.Description = $"[green]Processing Files[/] (Success: {successCount}, Failed: {0})";
                }

                for (int i = resumeFromIndex; i < allImageFiles.Length; i++)
                {
                    try
                    {
                        progressTask.Description = $"[green]Processing...[/] (Success: {successCount}, Failed: {failCount})";
                        var question = await _imageProcessor.ProcessImage(allImageFiles[i]).ConfigureAwait(false);
                        
                        if (question != null)
                        {
                            questions.Add(question);
                            successCount++;
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"[yellow]Warning: Failed to extract question from {Path.GetFileName(allImageFiles[i])}[/]");
                            failCount++;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Processing error for file {Path.GetFileName(allImageFiles[i])}: {ex.Message}[/]");
                        failCount++;
                    }
                    catch (IOException ex)
                    {
                        AnsiConsole.MarkupLine($"[red]IO error processing file {Path.GetFileName(allImageFiles[i])}: {ex.Message}[/]");
                        failCount++;
                    }

                    totalProcessed++;
                    progressTask.Increment(1);

                    if (totalProcessed % batchSize == 0 || i == allImageFiles.Length - 1)
                    {
                        ctx.Refresh();
                        AnsiConsole.MarkupLine($"[blue]Saving checkpoint at file index {i}...[/]");
                        await SaveQuestionsToJsonAsync(questions, outputFilePath).ConfigureAwait(false);
                    }
                }

                // Final summary
                AnsiConsole.MarkupLine($"[green]Processing complete![/]");
                AnsiConsole.MarkupLine($"[bold]Summary:[/] Total files: {allImageFiles.Length}, Success: {successCount}, Failed: {failCount}");
            }).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all PNG files from the directory, optionally limited to a specific count.
    /// </summary>
    /// <param name="directory">Directory to search for PNG files.</param>
    /// <param name="limit">Maximum number of files to return.</param>
    /// <returns>Array of file paths.</returns>
    private static string[] GetImageFiles(string directory, int? limit)
    {
        var files = Directory.GetFiles(directory, "*.png")
            .OrderBy(f => File.GetCreationTime(f))
            .ToArray();

        if (limit.HasValue && limit.Value > 0)
        {
            files = files.Take(limit.Value).ToArray();
        }

        return files;
    }

    /// <summary>
    /// Saves the questions list to a JSON file.
    /// </summary>
    /// <param name="questions">The questions to save.</param>
    /// <param name="outputPath">The path to save the JSON file to.</param>
    private static async Task SaveQuestionsToJsonAsync(List<BamfQuestion> questions, string outputPath)
    {
        try
        {
            // Create backup of existing file if it exists
            if (File.Exists(outputPath))
            {
                string backupPath = $"{outputPath}.bak";
                File.Copy(outputPath, backupPath, true);
                AnsiConsole.MarkupLine($"Created backup at [blue]{backupPath}[/]");
            }

            // Write to a temporary file first to prevent corruption if the process is interrupted
            string tempPath = $"{outputPath}.temp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(questions, JsonOptions)).ConfigureAwait(false);

            // Replace the original file with the temporary file
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            File.Move(tempPath, outputPath);
            AnsiConsole.MarkupLine($"Saved [green]{questions.Count}[/] questions to [blue]{outputPath}[/]");
        }
        catch (UnauthorizedAccessException ex)
        {
            AnsiConsole.MarkupLine($"[red]Access denied while saving questions: {ex.Message}[/]");
            throw;
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]IO error while saving questions: {ex.Message}[/]");
            if (File.Exists($"{outputPath}.temp"))
            {
                try
                {
                    File.Delete($"{outputPath}.temp");
                }
                catch (IOException deleteEx)
                {
                    AnsiConsole.MarkupLine($"[red]Failed to clean up temporary file: {deleteEx.Message}[/]");
                }
            }
            throw;
        }
    }
}