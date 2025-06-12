using System.Collections.Generic;
using System.Threading.Tasks;

namespace LanguageLearningTools.Domain
{
    /// <summary>
    /// Translates text from a source language to a target language asynchronously.
    /// </summary>
    public interface ISubtitleTranslationService
    {
        /// <summary>
        /// Translates a batch of subtitle lines from the source language to the target language.
        /// </summary>
        /// <param name="lines">The subtitle lines to translate (with context).</param>
        /// <param name="sourceLanguage">The source language.</param>
        /// <param name="targetLanguage">The target language.</param>
        /// <returns>The translated lines, preserving order and context.</returns>
        Task<IReadOnlyList<string>> TranslateBatchAsync(IReadOnlyList<string> lines, Lang sourceLanguage, Lang targetLanguage);
    }
}
