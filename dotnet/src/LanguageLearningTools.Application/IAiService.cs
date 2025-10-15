using System.Threading.Tasks;

namespace LanguageLearningTools.Application
{
    public interface IAiService
    {
        Task<string> GenerateContentAsync(string prompt, string? responseSchema = null);
    }
}
