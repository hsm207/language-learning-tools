using Microsoft.SemanticKernel;
using LanguageLearningTools.BAMFQuestionsToJson.Factories;
using LanguageLearningTools.BAMFQuestionsToJson.Services;
using Moq;
using Microsoft.Extensions.Configuration;

namespace LanguageLearningTools.BAMFQuestionsToJson.Tests;

public class KernelFactoryTests
{


    [Theory]
    [InlineData(null, "valid-key")]
    [InlineData("valid-model", null)]
    [InlineData(null, null)]
    public void CreateWithInvalidConfigurationReturnsNull(string? modelId, string? apiKey)
    {
        // Arrange
        var configServiceMock = new Mock<ConfigurationService>();
        configServiceMock.Setup(x => x.GetGoogleAiModelId()).Returns(modelId);
        configServiceMock.Setup(x => x.GetGoogleAiApiKey()).Returns(apiKey);
        configServiceMock.Setup(x => x.HasRequiredConfiguration()).Returns(false);

        // Act
        var kernel = KernelFactory.Create(configServiceMock.Object);

        // Assert
        Assert.Null(kernel);
        configServiceMock.Verify(x => x.HasRequiredConfiguration(), Times.Once);
    }

    [Fact]
    public void CreateWithValidConfigurationReturnsKernel()
    {
        // Arrange
        var configServiceMock = new Mock<ConfigurationService>();
        configServiceMock.Setup(x => x.GetGoogleAiModelId()).Returns("gemini-pro-vision");
        configServiceMock.Setup(x => x.GetGoogleAiApiKey()).Returns("test-api-key");
        configServiceMock.Setup(x => x.HasRequiredConfiguration()).Returns(true);

        // Act
        var kernel = KernelFactory.Create(configServiceMock.Object);

        // Assert
        Assert.NotNull(kernel);
        configServiceMock.Verify(x => x.HasRequiredConfiguration(), Times.Once);
        configServiceMock.Verify(x => x.GetGoogleAiModelId(), Times.Once);
        configServiceMock.Verify(x => x.GetGoogleAiApiKey(), Times.Once);
    }



    [Fact]
    public void CreateWithExceptionReturnsNullAndLogsError()
    {
        // Arrange
        var configServiceMock = new Mock<ConfigurationService>();
        configServiceMock.Setup(x => x.HasRequiredConfiguration()).Returns(true);
        configServiceMock.Setup(x => x.GetGoogleAiModelId()).Throws(new InvalidOperationException("Test exception"));

        // Act
        var kernel = KernelFactory.Create(configServiceMock.Object);

        // Assert
        Assert.Null(kernel);
        configServiceMock.Verify(x => x.HasRequiredConfiguration(), Times.Once);
        configServiceMock.Verify(x => x.GetGoogleAiModelId(), Times.Once);
    }

    [Fact]
    public void CreateWithEmptyStringsReturnsNull()
    {
        // Arrange
        var configServiceMock = new Mock<ConfigurationService>();
        configServiceMock.Setup(x => x.GetGoogleAiModelId()).Returns(string.Empty);
        configServiceMock.Setup(x => x.GetGoogleAiApiKey()).Returns(string.Empty);
        configServiceMock.Setup(x => x.HasRequiredConfiguration()).Returns(false);

        // Act
        var kernel = KernelFactory.Create(configServiceMock.Object);

        // Assert
        Assert.Null(kernel);
        configServiceMock.Verify(x => x.HasRequiredConfiguration(), Times.Once);
    }
}