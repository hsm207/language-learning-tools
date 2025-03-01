using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using LanguageLearningTools.BAMFQuestionsToJson.Models;
using Spectre.Console;

namespace LanguageLearningTools.BAMFQuestionsToJson.Commands;

/// <summary>
/// Command for processing a single image file to extract question data.
/// </summary>
internal sealed class ProcessImageCommand : CommandBase
{
    private readonly IImageProcessor _imageProcessor;
    private readonly string _imagePath;

    /// <summary>
    /// Initializes a new instance of the ProcessImageCommand class.
    /// </summary>
    /// <param name="imageProcessor">The image processor to use.</param>
    /// <param name="imagePath">The path to the image file to process.</param>
    public ProcessImageCommand(IImageProcessor imageProcessor, string imagePath)
        : base($"Process image {Path.GetFileName(imagePath)}")
    {
        _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
        _imagePath = imagePath ?? throw new ArgumentNullException(nameof(imagePath));
    }

    /// <summary>
    /// Executes the command to process a single image.
    /// </summary>
    /// <returns>A task representing the asynchronous operation that returns the processed question or null.</returns>
    public async Task<BamfQuestion?> ExecuteWithResultAsync()
    {
        try
        {
            return await _imageProcessor.ProcessImage(_imagePath).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Processing error for image {Path.GetFileName(_imagePath)}: {ex.Message}[/]");
            return null;
        }
        catch (FileNotFoundException ex)
        {
            AnsiConsole.MarkupLine($"[red]Image file not found {Path.GetFileName(_imagePath)}: {ex.Message}[/]");
            return null;
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]IO error reading image {Path.GetFileName(_imagePath)}: {ex.Message}[/]");
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            AnsiConsole.MarkupLine($"[red]Access denied to image {Path.GetFileName(_imagePath)}: {ex.Message}[/]");
            return null;
        }
    }

    /// <summary>
    /// Executes the command to process a single image.
    /// </summary>
    public override async Task ExecuteAsync()
    {
        await ExecuteWithResultAsync().ConfigureAwait(false);
    }
}