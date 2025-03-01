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
public sealed class ImageProcessor : IImageProcessor
{
    private readonly Kernel _kernel;
    private readonly SchemaService _schemaService;

    /// <summary>
    /// Initializes a new instance of the ImageProcessor class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance used for AI operations.</param>
    /// <param name="schemaService">The service used for schema operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when kernel is null.</exception>
    public ImageProcessor(Kernel kernel, SchemaService schemaService)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
    }

    /// <summary>
    /// Processes an image file containing a BAMF test question.
    /// </summary>
    /// <param name="imageFile">Path to the image file to process.</param>
    /// <returns>A BamfQuestion object containing the extracted question data, or null if processing fails.</returns>
    public async Task<BamfQuestion?> ProcessImage(string imageFile)
    {
#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var cleanSchema = _schemaService.CreateAndCleanSchema<BamfQuestion>();

        var executionSettings = new GeminiPromptExecutionSettings()
        {
            ResponseMimeType = "application/json",
            ResponseSchema = cleanSchema,
            Temperature = 1.0,
            TopP = 0.95,
            TopK = 40,
            MaxTokens = 8192,
        };
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var chatHistory = new ChatHistory();
        var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

        // Read the image file and convert to binary data
        var imageData = await ReadImageFile(imageFile);

        var prompt = GetPromptText();
        chatHistory.AddUserMessage(
        [
            new TextContent(prompt),
            new ImageContent(imageData, "image/jpeg"),
        ]);

        var reply = await chatCompletionService.GetChatMessageContentAsync(chatHistory, executionSettings);

        if (string.IsNullOrEmpty(reply.Content))
            return null;

        // Add a delay before returning
        await Task.Delay(TimeSpan.FromSeconds(12));

        return JsonSerializer.Deserialize<BamfQuestion>(reply.Content);
    }

    /// <summary>
    /// Reads an image file into a byte array.
    /// </summary>
    /// <param name="imageFile">Path to the image file.</param>
    /// <returns>The image file as a byte array.</returns>
    private static async Task<byte[]> ReadImageFile(string imageFile)
    {
        await using var stream = File.OpenRead(imageFile);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
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