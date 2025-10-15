using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Neovolve.Logging.Xunit;
using Xunit;
using Xunit.Abstractions;
using xRetry;

namespace LanguageLearningTools.Infrastructure.IntegrationTests
{
    public class GeminiSubtitleTranslationServiceIntegrationTests
    {
        private readonly string _apiKey;
        private readonly string _modelId = "gemini-2.5-flash-preview-05-20";
        private readonly ILoggerFactory _loggerFactory;
        private readonly GeminiSubtitleTranslationService _service;
        private readonly ITestOutputHelper _testOutputHelper;

        public GeminiSubtitleTranslationServiceIntegrationTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            var config = new ConfigurationBuilder()
                .AddUserSecrets<GeminiSubtitleTranslationServiceIntegrationTests>()
                .Build();
            _apiKey = config["GEMINI_API_KEY"];
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("GEMINI_API_KEY must be set in user secrets.");

            // Create logger factory that outputs to xUnit test output using Neovolve.Logging.Xunit
            // Change this to see debug logs during test execution
            _loggerFactory = LogFactory.Create(_testOutputHelper);

            var kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(modelId: _modelId, apiKey: _apiKey)
                .Build();

            var geminiKernelClient = new SemanticKernelGeminiClient(kernel);

            // For test determinism, set temperature to 0
            _service = new GeminiSubtitleTranslationService(geminiKernelClient, loggerFactory: _loggerFactory);
        }
    }
}
