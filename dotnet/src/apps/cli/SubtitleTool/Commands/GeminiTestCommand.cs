using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.Google.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SubtitleTool.Commands
{
    public class GeminiTestCommand : Command
    {
        public GeminiTestCommand() : base("gemini-test", "Runs a simple Gemini text generation example.")
        {
            var apiKeyOption = new Option<string>(
                name: "--api-key",
                description: "Google Gemini API key (optional; uses GEMINI_API_KEY environment variable if not provided)");

            AddOption(apiKeyOption);

            this.SetHandler(async (context) =>
            {
                var apiKey = context.ParseResult.GetValueForOption(apiKeyOption);
                await RunGeminiTest(apiKey);
            });
        }

        private async Task RunGeminiTest(string? apiKeyFromOption)
        {
            var resolvedApiKey = apiKeyFromOption ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            const string modelId = "gemini-2.5-flash";

            if (string.IsNullOrEmpty(resolvedApiKey))
            {
                Console.WriteLine("GEMINI_API_KEY environment variable not set, and no --api-key was provided.");
                return;
            }

            var kernel = Kernel.CreateBuilder()
                .AddGoogleAIGeminiChatCompletion(
                    modelId: modelId,
                    apiKey: resolvedApiKey,
                    apiVersion: GoogleAIVersion.V1_Beta
                )
                .Build();

            GeminiPromptExecutionSettings executionSettings = new()
            {
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(AbbreviationExpansionResponse)
            };

            Console.WriteLine("Generating structured JSON with Gemini...");
            var response = await kernel.InvokePromptAsync("What does LLM mean?", new(executionSettings));

            Console.WriteLine("Generated Structured JSON:");
            Console.WriteLine(response.ToString());

            try
            {
                var abbreviationResponse = JsonSerializer.Deserialize<AbbreviationExpansionResponse>(response.ToString());
                if (abbreviationResponse != null)
                {
                    Console.WriteLine($"Request: {abbreviationResponse.Request}");
                    Console.WriteLine($"Abbreviation: {abbreviationResponse.Abbreviation}");
                    Console.WriteLine($"Expansion: {abbreviationResponse.Expansion}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.GetType().Name} - {ex.Message}");
                if (ex is Microsoft.SemanticKernel.HttpOperationException httpOpEx)
                {
                    Console.WriteLine($"HTTP Response Content: {httpOpEx.ResponseContent}");
                }
            }
        }
    }
}
