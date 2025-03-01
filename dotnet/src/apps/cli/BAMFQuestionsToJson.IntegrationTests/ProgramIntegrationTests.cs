using System;
using System.CommandLine;
using System.Threading.Tasks;
using LanguageLearningTools.BAMFQuestionsToJson;
using LanguageLearningTools.BAMFQuestionsToJson.Interfaces;
using Moq;
using Xunit;

namespace BAMFQuestionsToJson.IntegrationTests
{
    public class ProgramIntegrationTests
    {
        [Fact]
        public async Task RunApplicationAsync_SuccessfulCommand_ReturnsZero()
        {
            // Arrange
            var args = new[] { "--help" };
            
            // Mock dependencies
            var mockServiceFactory = new Mock<IServiceFactory>();
            var mockLogger = new Mock<ILogger>();
            var mockErrorHandler = new Mock<IErrorHandler>();
            
            // Set up command line configuration mock
            var mockCommandLineConfig = new Mock<ICommandLineConfiguration>();
            var rootCommand = new RootCommand("Test command");
            
            mockCommandLineConfig
                .Setup(c => c.BuildRootCommand())
                .Returns(rootCommand);
                
            mockCommandLineConfig
                .Setup(c => c.RegisterCommandHandler(
                    It.IsAny<RootCommand>(), 
                    It.IsAny<IServiceFactory>()))
                .Callback<RootCommand, IServiceFactory>((cmd, factory) => {
                    // Add handlers that always succeed
                    cmd.SetHandler(() => Task.FromResult(0));
                });
                
            // Act
            int result = await Program.RunApplicationAsync(
                args,
                mockServiceFactory.Object,
                mockCommandLineConfig.Object,
                mockLogger.Object,
                mockErrorHandler.Object);
                
            // Assert
            Assert.Equal(0, result);
            mockCommandLineConfig.Verify(c => c.BuildRootCommand(), Times.Once);
            mockCommandLineConfig.Verify(
                c => c.RegisterCommandHandler(
                    It.IsAny<RootCommand>(), 
                    It.IsAny<IServiceFactory>()),
                Times.Once);
        }
        
        [Fact]
        public async Task RunApplicationAsync_ExceptionThrown_HandlesError()
        {
            // Arrange
            var args = new[] { "--invalid-command" };
            
            var mockServiceFactory = new Mock<IServiceFactory>();
            var mockLogger = new Mock<ILogger>();
            
            var mockCommandLineConfig = new Mock<ICommandLineConfiguration>();
            mockCommandLineConfig
                .Setup(c => c.BuildRootCommand())
                .Throws(new Exception("Test exception"));
                
            var mockErrorHandler = new Mock<IErrorHandler>();
            mockErrorHandler
                .Setup(h => h.HandleException(It.IsAny<Exception>()))
                .Returns(1);
                
            // Act
            int result = await Program.RunApplicationAsync(
                args,
                mockServiceFactory.Object,
                mockCommandLineConfig.Object,
                mockLogger.Object,
                mockErrorHandler.Object);
                
            // Assert
            Assert.Equal(1, result);
            mockErrorHandler.Verify(h => h.HandleException(It.IsAny<Exception>()), Times.Once);
        }
    }
}
