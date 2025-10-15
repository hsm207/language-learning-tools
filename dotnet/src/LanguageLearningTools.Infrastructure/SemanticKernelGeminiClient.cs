using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using LanguageLearningTools.Application;

namespace LanguageLearningTools.Infrastructure
{
    /// <summary>
    /// Concrete implementation of IGeminiKernelClient using Microsoft.SemanticKernel.
    /// </summary>
    public class SemanticKernelGeminiClient : IGeminiKernelClient
    {
        private readonly Kernel _kernel;

        public SemanticKernelGeminiClient(Kernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<string> InvokeGeminiAsync(string prompt, GeminiPromptExecutionSettings executionSettings)
        {
            var promptConfig = new PromptTemplateConfig(prompt);
            var promptFunction = _kernel.CreateFunctionFromPrompt(promptConfig);

            var result = await promptFunction.InvokeAsync(_kernel, new KernelArguments(executionSettings));

            return result.GetValue<string>() ?? string.Empty;
        }
    }
}
