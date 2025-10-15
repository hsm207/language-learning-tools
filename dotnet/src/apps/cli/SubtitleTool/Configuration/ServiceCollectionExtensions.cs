using Microsoft.SemanticKernel.Connectors.Google;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using LanguageLearningTools.Application;
using LanguageLearningTools.Domain;
using LanguageLearningTools.Infrastructure;

namespace SubtitleTool.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddTranslationServices(this IServiceCollection services, string? apiKey, bool verbose, int requestsPerMinute)
        {
            // Register logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                if (verbose)
                {
                    builder.SetMinimumLevel(LogLevel.Debug);
                }
                else
                {
                    builder.SetMinimumLevel(LogLevel.Information);
                }
            });

            var resolvedApiKey = apiKey ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrEmpty(resolvedApiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY environment variable not set, and no --api-key was provided.");
            }

            // Register Semantic Kernel with Gemini
            var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
            kernelBuilder.AddGoogleAIGeminiChatCompletion("gemini-flash-lite-latest", resolvedApiKey, GoogleAIVersion.V1_Beta);
#pragma warning restore SKEXP0070
            var kernel = kernelBuilder.Build();
            services.AddSingleton(kernel); // Register Kernel as a singleton

            // Register Gemini Kernel Client
            services.AddSingleton<IGeminiKernelClient, SemanticKernelGeminiClient>();

            // Register AI service
            services.AddTransient<IAiService>(provider =>
            {
                var k = provider.GetRequiredService<Kernel>(); // Get Kernel from DI
                return new SemanticKernelAiService(k);
            });

            // Register domain services
            services.AddTransient<ISubtitleBatchingStrategy, RollingWindowBatchingStrategy>();

            // Register infrastructure services
            services.AddTransient<ISubtitleParser, TtmlSubtitleParser>();
            services.AddTransient<ISubtitleTranslationService>(provider =>
            {
                var geminiKernelClient = provider.GetRequiredService<IGeminiKernelClient>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                // Convert requests per minute to delay in milliseconds
                var delayMs = (int)(60_000.0 / requestsPerMinute);
                var delay = TimeSpan.FromMilliseconds(delayMs);
                return new GeminiSubtitleTranslationService(geminiKernelClient, temperature: 0.2, requestDelay: delay, loggerFactory: loggerFactory);
            });

            // Register application services
            services.AddTransient<SubtitleTranslationApplicationService>();

            return services;
        }
    }
}
