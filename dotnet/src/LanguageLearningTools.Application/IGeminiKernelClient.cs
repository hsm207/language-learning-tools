using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LanguageLearningTools.Application
{
    /// <summary>
    /// Abstraction for interacting with the Gemini API via Semantic Kernel.
    /// </summary>
    public interface IGeminiKernelClient
    {
        /// <summary>
        /// Invokes the Gemini API with a given prompt and execution settings.
        /// </summary>
        /// <param name="prompt">The prompt string to send to Gemini.</param>
        /// <param name="executionSettings">The execution settings for the Gemini API call.</param>
        /// <returns>The raw string response from the Gemini API.</returns>
        Task<string> InvokeGeminiAsync(string prompt, GeminiPromptExecutionSettings executionSettings);
    }
}
