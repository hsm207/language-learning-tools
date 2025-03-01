using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using LanguageLearningTools.BAMFQuestionsToJson.Models;
using Spectre.Console;
using System.Text.Json;
using System.Security;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// Command for processing multiple image files in a batch operation.
/// </summary>
internal sealed class ProcessFileBatchCommand : CommandBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IImageProcessor _imageProcessor;
    private readonly string _inputDirectory;
    private readonly string _outputFilePath;
    private readonly int? _limit;
    private readonly int _batchSize;
    private readonly CommandInvoker _commandInvoker;

    /// <summary>
    /// Initializes a new instance of the ProcessFileBatchCommand class.
    /// </summary>
    /// <param name="imageProcessor">The image processor to use.</param>
    /// <param name="inputDirectory">Directory containing question screenshots.</param>
    /// <param name="outputFilePath">Path where the JSON output file will be saved.</param>
    /// <param name="limit">Maximum number of files to process (optional).</param>
    /// <param name="batchSize">Number of files to process before saving interim results.</param>
    public ProcessFileBatchCommand(
        IImageProcessor imageProcessor,
        string inputDirectory,
        string outputFilePath,
        int? limit,
        int batchSize)
        : base($"Process files from {inputDirectory}")
    {
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _inputDirectory = inputDirectory ?? throw new ArgumentNullException(nameof(inputDirectory));
        _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
        _limit = limit;
        _batchSize = batchSize;
        _commandInvoker = new CommandInvoker();
    }

    /// <summary>
    /// Executes the batch processing command.
    /// </summary>
    public override async Task ExecuteAsync()
    {
        AnsiConsole.MarkupLine($"Processing screenshots from: [green]{_inputDirectory}[/]");
        AnsiConsole.MarkupLine($"Output will be saved to: [green]{_outputFilePath}[/]");

        if (_limit.HasValue)
        {
            AnsiConsole.MarkupLine($"Processing up to [yellow]{_limit}[/] files");
        }

        AnsiConsole.MarkupLine($"Using batch size of [blue]{_batchSize}[/] files");

        // Create processed subfolder if it doesn't exist
        string processedFolder = Path.Combine(_inputDirectory, "processed");
        if (!Directory.Exists(processedFolder))
        {
            Directory.CreateDirectory(processedFolder);
            AnsiConsole.MarkupLine($"Created [blue]{processedFolder}[/] directory for processed images");
        }

        // Get all PNG files sorted by creation time
        string[] allImageFiles = GetImageFiles(_inputDirectory, _limit);

        if (allImageFiles.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No image files found to process.[/]");
            return;
        }

        AnsiConsole.MarkupLine($"Found [green]{allImageFiles.Length}[/] files to process");

        // Load existing questions if resuming and the output file exists
        var questions = new List<BamfQuestion>();

        // Process files in batches
        int totalProcessed = 0;
        int successCount = 0;
        int failCount = 0;

        // Keep track of successfully processed files for moving later
        var successfullyProcessedFiles = new List<string>();

        // Define the progress data
        var totalFiles = allImageFiles.Length;
        var filesRemaining = totalFiles;

        // Process files with progress tracking
        await AnsiConsole.Progress()
            .AutoClear(false)  // Do not remove the task list when done
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),    // Task description
                new ProgressBarColumn(),        // Progress bar
                new PercentageColumn(),         // Percentage
                new RemainingTimeColumn(),      // Remaining time
                new SpinnerColumn(),            // Spinner
            })
            .StartAsync(async ctx =>
            {
                // Define task
                var progressTask = ctx.AddTask($"[green]Processing Files[/]", maxValue: filesRemaining);

                // Process each file
                for (int i = 0; i < allImageFiles.Length; i++)
                {
                    // Update progress description with current stats
                    progressTask.Description = $"[green]Processing...[/] (Success: {successCount}, Failed: {failCount})";

                    // Create a command for processing this specific image
                    var command = new ProcessImageCommand(_imageProcessor, allImageFiles[i]);
                    var question = await command.ExecuteWithResultAsync().ConfigureAwait(false);

                    if (question != null)
                    {
                        questions.Add(question);
                        successCount++;
                        successfullyProcessedFiles.Add(allImageFiles[i]);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: Failed to extract question from {Path.GetFileName(allImageFiles[i])}[/]");
                        failCount++;
                    }

                    totalProcessed++;
                    progressTask.Increment(1);

                    // Save checkpoint after each batch
                    if (totalProcessed % _batchSize == 0 || i == allImageFiles.Length - 1)
                    {
                        // Pause the task momentarily to show the checkpoint message
                        ctx.Refresh();
                        AnsiConsole.MarkupLine($"[blue]Saving checkpoint at file index {i}...[/]");

                        await SaveQuestionsAsync(questions, _outputFilePath).ConfigureAwait(false);

                        // Move successfully processed files to the processed folder
                        foreach (string processedFile in successfullyProcessedFiles)
                        {
                            try
                            {
                                string processedFileName = Path.GetFileName(processedFile);
                                string destPath = Path.Combine(processedFolder, processedFileName);

                                // If a file with the same name exists in the processed folder, add a timestamp
                                if (File.Exists(destPath))
                                {
                                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
                                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(processedFileName);
                                    string extension = Path.GetExtension(processedFileName);
                                    destPath = Path.Combine(processedFolder, $"{fileNameWithoutExt}_{timestamp}{extension}");
                                }

                                File.Move(processedFile, destPath);
                                AnsiConsole.MarkupLine($"Moved [green]{processedFileName}[/] to processed folder");
                            }
                            catch (IOException ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error moving file {Path.GetFileName(processedFile)}: {ex.Message}[/]");
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Access denied when moving file {Path.GetFileName(processedFile)}: {ex.Message}[/]");
                            }
                            catch (SecurityException ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Security error moving file {Path.GetFileName(processedFile)}: {ex.Message}[/]");
                            }
                        }

                        // Clear the list after moving files
                        successfullyProcessedFiles.Clear();
                    }
                }
            }).ConfigureAwait(false);

        // Final summary
        AnsiConsole.MarkupLine($"[green]Processing complete![/]");
        AnsiConsole.MarkupLine($"[bold]Summary:[/] Total files: {allImageFiles.Length}, Success: {successCount}, Failed: {failCount}");
    }

    /// <summary>
    /// Gets all PNG files from the directory, optionally limited to a specific count.
    /// Excludes files from the 'processed' subfolder.
    /// </summary>
    private static string[] GetImageFiles(string directory, int? limit)
    {
        var files = Directory.GetFiles(directory, "*.png")
            .Where(f => !f.Contains(Path.Combine(directory, "processed"), StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => File.GetCreationTime(f))
            .ToArray();

        if (limit.HasValue && limit.Value > 0)
        {
            files = files.Take(limit.Value).ToArray();
        }

        return files;
    }

    /// <summary>
    /// Saves the list of questions to a JSON file. If the file exists, merges new questions with existing ones.
    /// </summary>
    private static async Task SaveQuestionsAsync(List<BamfQuestion> newQuestions, string outputFilePath)
    {
        List<BamfQuestion> allQuestions = new();

        // Read existing questions if file exists
        if (File.Exists(outputFilePath))
        {
            try
            {
                using var existingFileStream = File.OpenRead(outputFilePath);
                var existingQuestions = await JsonSerializer.DeserializeAsync<List<BamfQuestion>>(existingFileStream, JsonOptions).ConfigureAwait(false);
                if (existingQuestions != null)
                {
                    allQuestions.AddRange(existingQuestions);
                }
            }
            catch (JsonException ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Could not parse existing questions file: {ex.Message}[/]");
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning: Could not read existing questions file: {ex.Message}[/]");
            }
        }

        // Add new questions, avoiding duplicates based on QuestionNumber
        foreach (var question in newQuestions)
        {
            if (!allQuestions.Any(q => q.QuestionNumber == question.QuestionNumber))
            {
                allQuestions.Add(question);
            }
        }

        // Create a temporary file for writing
        var tempFilePath = outputFilePath + ".tmp";
        try
        {
            using (var stream = File.Create(tempFilePath))
            {
                await JsonSerializer.SerializeAsync(stream, allQuestions, JsonOptions).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }

            // Replace the original file with the temporary file
            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
            File.Move(tempFilePath, outputFilePath);
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving questions file: {ex.Message}[/]");
            if (File.Exists(tempFilePath))
            {
                try 
                { 
                    File.Delete(tempFilePath); 
                } 
                catch (IOException deleteEx) 
                { 
                    AnsiConsole.MarkupLine($"[red]Failed to clean up temporary file: {deleteEx.Message}[/]"); 
                }
                catch (UnauthorizedAccessException deleteEx)
                {
                    AnsiConsole.MarkupLine($"[red]Access denied while cleaning up temporary file: {deleteEx.Message}[/]");
                }
            }
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            AnsiConsole.MarkupLine($"[red]Access denied while saving questions file: {ex.Message}[/]");
            throw;
        }
    }
}