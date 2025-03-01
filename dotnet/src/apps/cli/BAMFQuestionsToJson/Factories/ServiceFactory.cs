using LanguageLearningTools.BAMFQuestionsToJson.Commands;
using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using LanguageLearningTools.BAMFQuestionsToJson.Services;
using Microsoft.SemanticKernel;

namespace LanguageLearningTools.BAMFQuestionsToJson.Factories;

/// <summary>
/// Factory that creates service dependencies for the application.
/// </summary>
public class ServiceFactory : IServiceFactory
{
    private readonly ConfigurationService _configurationService;

    /// <summary>
    /// Initializes a new instance of the ServiceFactory class.
    /// </summary>
    public ServiceFactory()
    {
        _configurationService = new ConfigurationService();
    }

    /// <summary>
    /// Initializes a new instance of the ServiceFactory class with a specific configuration service.
    /// </summary>
    /// <param name="configurationService">The configuration service to use.</param>
    public ServiceFactory(ConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
    }

    /// <summary>
    /// Creates a Semantic Kernel instance.
    /// </summary>
    /// <returns>A configured Kernel instance or null if configuration is unavailable.</returns>
    public Kernel? CreateKernel()
    {
        return KernelFactory.Create(_configurationService);
    }

    /// <summary>
    /// Creates a SchemaService instance.
    /// </summary>
    /// <returns>A new SchemaService instance.</returns>
    public SchemaService CreateSchemaService()
    {
        return new SchemaService();
    }

    /// <summary>
    /// Creates an ImageProcessor instance.
    /// </summary>
    /// <returns>A configured IImageProcessor instance.</returns>
    public IImageProcessor CreateImageProcessor()
    {
        var kernel = CreateKernel();
        if (kernel == null)
        {
            throw new InvalidOperationException("Failed to create Semantic Kernel instance");
        }

        var schemaService = CreateSchemaService();
        return new ImageProcessor(kernel, schemaService);
    }

    /// <summary>
    /// Creates a CommandInvoker instance.
    /// </summary>
    /// <returns>A new CommandInvoker instance.</returns>
    public CommandInvoker CreateCommandInvoker()
    {
        return new CommandInvoker();
    }

    /// <summary>
    /// Creates a ProcessFileBatchCommand instance.
    /// </summary>
    /// <param name="imageProcessor">The image processor to use.</param>
    /// <param name="inputDirectory">Directory containing question screenshots.</param>
    /// <param name="outputFilePath">Path where the JSON output file will be saved.</param>
    /// <param name="limit">Maximum number of files to process (optional).</param>
    /// <param name="batchSize">Number of files to process before saving interim results.</param>
    /// <returns>A configured ProcessFileBatchCommand instance.</returns>
    public ProcessFileBatchCommand CreateProcessFileBatchCommand(
        IImageProcessor imageProcessor,
        string inputDirectory,
        string outputFilePath,
        int? limit,
        int batchSize)
    {
        return new ProcessFileBatchCommand(
            imageProcessor,
            inputDirectory,
            outputFilePath,
            limit,
            batchSize);
    }
}