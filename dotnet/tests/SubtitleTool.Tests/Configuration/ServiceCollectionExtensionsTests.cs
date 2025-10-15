
using Microsoft.Extensions.DependencyInjection;
using SubtitleTool.Configuration;
using Xunit;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Moq;

namespace SubtitleTool.Tests.Configuration;

public class ServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Verify that all necessary translation services are set up correctly")]
    public void AddTranslationServices_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-api-key");

        // Act
        services.AddTranslationServices(null, false, 1000);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<ISubtitleParser>());
        Assert.IsType<TtmlSubtitleParser>(serviceProvider.GetService<ISubtitleParser>());

        Assert.NotNull(serviceProvider.GetService<ISubtitleBatchingStrategy>());
        Assert.IsType<RollingWindowBatchingStrategy>(serviceProvider.GetService<ISubtitleBatchingStrategy>());

        Assert.NotNull(serviceProvider.GetService<ISubtitleTranslationService>());
        Assert.IsType<GeminiSubtitleTranslationService>(serviceProvider.GetService<ISubtitleTranslationService>());
    }

    [Fact(DisplayName = "Verify that a mock translation service can be used for testing purposes")]
    public void AddTranslationServices_WithMock_ShouldRegisterMockServices()
    {
        // Arrange
        var services = new ServiceCollection();
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "test-api-key");

        // Act
        services.AddTranslationServices(null, true, 1000);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(serviceProvider.GetService<ISubtitleTranslationService>());
        Assert.IsAssignableFrom<ISubtitleTranslationService>(new Mock<ISubtitleTranslationService>().Object);
    }
}
