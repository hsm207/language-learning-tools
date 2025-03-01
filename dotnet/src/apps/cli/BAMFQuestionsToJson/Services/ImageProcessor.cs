using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using LanguageLearningTools.BAMFQuestionsToJson.Models;
using LanguageLearningTools.BAMFQuestionsToJson.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;

namespace LanguageLearningTools.BAMFQuestionsToJson.Services;

/// <summary>
/// Processes image files containing BAMF test questions using the Semantic Kernel chat completion service.
/// Extracts question content from images using AI-powered image recognition.
/// </summary>
internal sealed class ImageProcessor : IImageProcessor
{
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of the ImageProcessor class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance used for AI operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when kernel is null.</exception>
    public ImageProcessor(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    /// <summary>
    /// Processes an image file containing a BAMF test question.
    /// </summary>
    /// <param name="imageFile">Path to the image file to process.</param>
    /// <returns>A BamfQuestion object containing the extracted question data, or null if processing fails.</returns>
    public async Task<BamfQuestion?> ProcessImage(string imageFile)
    {
        try
        {
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            var cleanSchema = SchemaService.CreateAndCleanSchema<BamfQuestion>();

            var executionSettings = new GeminiPromptExecutionSettings()
            {
                ResponseMimeType = "application/json",
                ResponseSchema = cleanSchema,
                Temperature = 1.0,
                TopP = 0.95,
                TopK = 40,
                MaxTokens = 8192,
            };
#pragma warning restore SKEXP0070

            var chatHistory = new ChatHistory();
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            // Read the image file and convert to binary data
            var imageData = await ReadImageFile(imageFile).ConfigureAwait(false);

            var prompt = GetPromptText();
            chatHistory.AddUserMessage(
            [
                new TextContent(prompt),
                new ImageContent(imageData, "image/jpeg"),
            ]);

            var reply = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings).ConfigureAwait(false);

            if (string.IsNullOrEmpty(reply.Content))
                return null;

            // Add a delay before returning
            await Task.Delay(TimeSpan.FromSeconds(12)).ConfigureAwait(false);

            return JsonSerializer.Deserialize<BamfQuestion>(reply.Content);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse AI response for image {imageFile}: {ex.Message}", ex);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to process image {imageFile}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reads an image file into a byte array.
    /// </summary>
    /// <param name="imageFile">Path to the image file.</param>
    /// <returns>The image file as a byte array.</returns>
    private static async Task<byte[]> ReadImageFile(string imageFile)
    {
        try
        {
            if (!File.Exists(imageFile))
            {
                throw new FileNotFoundException($"Image file not found: {imageFile}");
            }

            using var stream = File.OpenRead(imageFile);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            return memoryStream.ToArray();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied to image file {imageFile}: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"IO error reading image file {imageFile}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the prompt text for the AI model.
    /// </summary>
    private static string GetPromptText()
    {
        return """
        Help me study for the German Citizenship test by answering these questions. Extract the question and possible answers. then answer it. Provide justification in a super engaging way e.g using emojis, attractive formatting, etc using naughy/sexy/sexual examples to maximize lerarner's understanding and recall including additional interesting and relevant fats about the topic(s) the question is about. Respond in the given json schema.
        """;
    }
}