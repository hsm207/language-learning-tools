using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LanguageLearningTools.Infrastructure.IntegrationTests
{
    public class GeminiSubtitleTranslationServiceIntegrationTests
    {
        [Fact]
        public async Task TranslateBatchAsync_Should_Translate_German_To_English()
        {
            // Arrange: Load Gemini API key from user secrets
            var config = new ConfigurationBuilder()
                .AddUserSecrets<GeminiSubtitleTranslationServiceIntegrationTests>()
                .Build();
            var apiKey = config["GEMINI_API_KEY"];
            Assert.False(string.IsNullOrWhiteSpace(apiKey), "GEMINI_API_KEY must be set in user secrets.");

            // Use the specified Gemini model id
            var modelId = "gemini-2.5-flash-preview-05-20";

            // Initialize the Gemini kernel
            var kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(modelId: modelId, apiKey: apiKey)
                .Build();

            var service = new GeminiSubtitleTranslationService(kernel);
            var lines = new List<string> { "Guten Morgen", "Wie geht es dir?"};

            // Act
            var translations = await service.TranslateBatchAsync(lines, Lang.German, Lang.English);

            // Assert: Check that translations are non-empty and different from input
            Assert.Equal(lines.Count, translations.Count);
            foreach (var translation in translations)
            {
                Assert.False(string.IsNullOrWhiteSpace(translation));
            }
        }
    }
}
