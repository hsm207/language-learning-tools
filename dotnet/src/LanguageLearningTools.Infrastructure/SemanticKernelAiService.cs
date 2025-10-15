using System.Threading.Tasks;
using LanguageLearningTools.Application;
using Microsoft.SemanticKernel;

namespace LanguageLearningTools.Infrastructure
{
    public class SemanticKernelAiService : IAiService
    {
        private readonly Kernel _kernel;

        public SemanticKernelAiService(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<string> GenerateContentAsync(string prompt, string? responseSchema = null)
        {
            var promptConfig = new PromptTemplateConfig(prompt);
            var promptFunction = _kernel.CreateFunctionFromPrompt(promptConfig);

            var executionSettings = new Microsoft.SemanticKernel.Connectors.Google.GeminiPromptExecutionSettings
            {
                ResponseMimeType = responseSchema != null ? "application/json" : "text/plain",
                ResponseSchema = responseSchema
            };

            var result = await promptFunction.InvokeAsync(_kernel, new KernelArguments(executionSettings));

            return result.GetValue<string>() ?? string.Empty;
        }
    }
}
